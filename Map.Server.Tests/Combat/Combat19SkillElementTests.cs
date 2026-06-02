using System;
using Core.Database.Entities;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Spawn;
using Map.Server.Status;
using AttrFixDbEntity = Core.Database.Entities.AttrFixDbEntity;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-19 — per-skill element resolution. Magic/misc damage resolves the
/// attack element from skill_db (battle_get_magic/misc_element, battle.cpp:3582/
/// 3675) with the ELE_WEAPON / ELE_ENDOWED / ELE_RANDOM sentinels, not the
/// caster's weapon element; the resolved element also drives the defender
/// bSubEle resist lookup.
/// </summary>
public class Combat19SkillElementTests
{
    static Combat19SkillElementTests()
    {
        // ElementTable.Initialize REPLACES the global matrix; seed the same
        // superset BattleCalculatorTests uses so parallel class ordering can't
        // wipe an entry another class needs (Water→Fire 150 here).
        ElementTable.Initialize(new[]
        {
            new AttrFixDbEntity { Level = 1, AttackerElement = "Fire", DefenderElement = "Water", Multiplier = 90 },
            new AttrFixDbEntity { Level = 1, AttackerElement = "Water", DefenderElement = "Fire", Multiplier = 150 },
        });
    }

    // ---- BattleElementService: concrete element from skill_db ----

    [Fact]
    public void Magic_resolves_declared_skill_element()
    {
        var svc = new BattleElementService(new SkillDb());
        var caster = NewPlayer(weaponElement: BattleElement.Neutral);

        // Fire Bolt declares Element: Fire — resolves Fire regardless of weapon.
        Assert.Equal(BattleElement.Fire, svc.GetMagicElement(caster, SkillIds.MG_FIREBOLT, 5));
        // Cold Bolt declares Element: Water.
        Assert.Equal(BattleElement.Water, svc.GetMagicElement(caster, SkillIds.MG_COLDBOLT, 5));
    }

    [Fact]
    public void Magic_unknown_skill_is_neutral()
    {
        var svc = new BattleElementService(new SkillDb());
        Assert.Equal(BattleElement.Neutral, svc.GetMagicElement(NewPlayer(BattleElement.Fire), 64000, 1));
    }

    [Fact]
    public void Misc_resolves_declared_element()
    {
        var svc = new BattleElementService(new SkillDb());
        Assert.Equal(BattleElement.Fire, svc.GetMiscElement(NewPlayer(BattleElement.Neutral), SkillIds.MG_FIREBOLT, 1));
    }

    // ---- sentinels: ELE_WEAPON / ELE_ENDOWED / ELE_RANDOM ----

    [Fact]
    public void Magic_ele_weapon_takes_weapon_element()
    {
        var svc = new BattleElementService(new SentinelSkillDb(1000, BattleElement.Weapon));
        var caster = NewPlayer(weaponElement: BattleElement.Wind);
        Assert.Equal(BattleElement.Wind, svc.GetMagicElement(caster, 1000, 1));
    }

    [Fact]
    public void Magic_ele_endowed_takes_weapon_element()
    {
        // Endows update Stats.WeaponElement in this engine (SC-02/SC-11), so
        // ELE_ENDOWED reads the same source.
        var svc = new BattleElementService(new SentinelSkillDb(1000, BattleElement.Endowed));
        Assert.Equal(BattleElement.Fire, svc.GetMagicElement(NewPlayer(BattleElement.Fire), 1000, 1));
    }

    [Fact]
    public void Magic_ele_random_is_in_range()
    {
        var svc = new BattleElementService(new SentinelSkillDb(1000, BattleElement.Random), new Random(0));
        for (var i = 0; i < 50; i++)
        {
            var e = svc.GetMagicElement(NewPlayer(BattleElement.Neutral), 1000, 1);
            Assert.InRange((int)e, (int)BattleElement.Neutral, (int)BattleElement.Undead);
        }
    }

    [Fact]
    public void Misc_ele_weapon_and_endowed_force_neutral()
    {
        var caster = NewPlayer(BattleElement.Fire);
        Assert.Equal(BattleElement.Neutral,
            new BattleElementService(new SentinelSkillDb(1000, BattleElement.Weapon)).GetMiscElement(caster, 1000, 1));
        Assert.Equal(BattleElement.Neutral,
            new BattleElementService(new SentinelSkillDb(1000, BattleElement.Endowed)).GetMiscElement(caster, 1000, 1));
    }

    // ---- integration: resolved element reaches the damage rate ----

    [Fact]
    public void Magic_damage_uses_resolved_element_rate()
    {
        // Fire Bolt vs a Water-element target → Fire→Water 90% even though the
        // caster's weapon is Neutral. (A broken resolver would give Neutral→
        // Water = 100%.)
        var calc = new BattleCalculator(rng: new Random(0), cards: null, sc: null, mado: null,
            elements: new BattleElementService(new SkillDb()));
        var caster = NewPlayer(BattleElement.Neutral);
        caster.Stats.MatkMin = caster.Stats.MatkMax = 200;
        var target = NewWaterTarget();

        var dmg = calc.CalcMagicAttack(caster, target, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100);

        Assert.Equal(180, dmg.Damage); // 200 × 90%
    }

    [Fact]
    public void Magic_subele_resist_uses_resolved_element()
    {
        // Defender carries bSubEle,Fire 20 → a Fire Bolt is reduced 20%.
        var cards = new BattleCardService(Microsoft.Extensions.Logging.Abstractions.NullLogger<BattleCardService>.Instance);
        var calc = new BattleCalculator(rng: new Random(0), cards: cards, sc: null, mado: null,
            elements: new BattleElementService(new SkillDb()));
        var caster = NewPlayer(BattleElement.Neutral);
        caster.Stats.MatkMin = caster.Stats.MatkMax = 200;
        var target = NewPlayerTarget();          // Neutral defense element → 100% table
        target.EquipBonuses.SubEle[(int)BattleElement.Fire] = 20;

        var dmg = calc.CalcMagicAttack(caster, target, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100);

        Assert.Equal(160, dmg.Damage); // 200 × (100 - 20)%
    }

    // ---- helpers ----

    private static PlayerEntity NewPlayer(BattleElement weaponElement)
    {
        var p = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        p.Stats.WeaponElement = (byte)weaponElement;
        return p;
    }

    private static MobEntity NewWaterTarget()
    {
        var t = MakeMob();
        t.Stats.DefenseElement = BattleElement.Water;
        t.Stats.ElementLevel = 1;
        return t;
    }

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Mdef = 0; m.Stats.Mdef2 = 0;
        return m;
    }

    private static PlayerEntity NewPlayerTarget()
    {
        var p = new PlayerEntity(2, 2, "Tank", Guid.NewGuid(), 0, 0, 0);
        p.Stats.DefenseElement = BattleElement.Neutral; p.Stats.ElementLevel = 1;
        p.Stats.Mdef = 0; p.Stats.Mdef2 = 0;
        return p;
    }

    /// <summary>ISkillDb that returns one crafted definition (for the element
    /// sentinels) and delegates every other member to a real fallback SkillDb.</summary>
    private sealed class SentinelSkillDb : ISkillDb
    {
        private readonly SkillDb _inner = new();
        private readonly ushort _id;
        private readonly SkillDefinition _def;

        public SentinelSkillDb(ushort id, BattleElement element)
        {
            _id = id;
            _def = new SkillDefinition
            {
                Id = id, Name = "Sentinel", MaxLevel = 10, Element = element,
                Target = SkillTargetMode.TargetEnemy, DamageKind = SkillDamageKind.Magic,
            };
        }

        public SkillDefinition? Get(ushort skillId) => skillId == _id ? _def : _inner.Get(skillId);
        public BattleElement GetEle(ushort skillId) => skillId == _id ? _def.Element : _inner.GetEle(skillId);

        // --- delegation for the rest of ISkillDb ---
        public int Count => _inner.Count;
        public void Reload() => _inner.Reload();
        public void LoadingFinished() => _inner.LoadingFinished();
        public ReadOnlySpan<ushort> GetCombo(ushort skillId) => _inner.GetCombo(skillId);
        public int GetMaxLevel(ushort skillId) => _inner.GetMaxLevel(skillId);
        public int GetRange(ushort skillId) => _inner.GetRange(skillId);
        public int GetRange2(ushort skillId, ushort level) => _inner.GetRange2(skillId, level);
        public int GetHp(ushort skillId, ushort level) => _inner.GetHp(skillId, level);
        public int GetSp(ushort skillId, ushort level) => _inner.GetSp(skillId, level);
        public int GetHpRate(ushort skillId, ushort level) => _inner.GetHpRate(skillId, level);
        public int GetSpRate(ushort skillId, ushort level) => _inner.GetSpRate(skillId, level);
        public int GetAp(ushort skillId, ushort level) => _inner.GetAp(skillId, level);
        public int GetApRate(ushort skillId, ushort level) => _inner.GetApRate(skillId, level);
        public int GetGiveAp(ushort skillId, ushort level) => _inner.GetGiveAp(skillId, level);
        public int GetMhp(ushort skillId, ushort level) => _inner.GetMhp(skillId, level);
        public int GetZeny(ushort skillId, ushort level) => _inner.GetZeny(skillId, level);
        public int GetSpiritBall(ushort skillId, ushort level) => _inner.GetSpiritBall(skillId, level);
        public int GetNum(ushort skillId, ushort level) => _inner.GetNum(skillId, level);
        public int GetBlewCount(ushort skillId, ushort level) => _inner.GetBlewCount(skillId, level);
        public int GetCast(ushort skillId, ushort level) => _inner.GetCast(skillId, level);
        public int GetFixedCast(ushort skillId, ushort level) => _inner.GetFixedCast(skillId, level);
        public int GetDelay(ushort skillId, ushort level) => _inner.GetDelay(skillId, level);
        public int GetWalkDelay(ushort skillId, ushort level) => _inner.GetWalkDelay(skillId, level);
        public int GetCooldown(ushort skillId, ushort level) => _inner.GetCooldown(skillId, level);
        public int GetTime(ushort skillId, ushort level) => _inner.GetTime(skillId, level);
        public int GetTime2(ushort skillId, ushort level) => _inner.GetTime2(skillId, level);
        public int GetTime3(ushort skillId, ushort level) => _inner.GetTime3(skillId, level);
        public int GetCastDef(ushort skillId) => _inner.GetCastDef(skillId);
        public bool GetCastCancel(ushort skillId) => _inner.GetCastCancel(skillId);
        public int GetCastNoDex(ushort skillId) => _inner.GetCastNoDex(skillId);
        public int GetDelayNoDex(ushort skillId) => _inner.GetDelayNoDex(skillId);
        public int GetNoCast(ushort skillId) => _inner.GetNoCast(skillId);
        public int GetMaxCount(ushort skillId, ushort level) => _inner.GetMaxCount(skillId, level);
        public int GetState(ushort skillId) => _inner.GetState(skillId);
        public int GetType(ushort skillId) => _inner.GetType(skillId);
        public SkillTargetMode GetInf(ushort skillId) => _inner.GetInf(skillId);
        public bool GetInf2(ushort skillId, SkillInf2 flag) => _inner.GetInf2(skillId, flag);
        public bool GetNk(ushort skillId, SkillNk flag) => _inner.GetNk(skillId, flag);
        public int GetWeaponType(ushort skillId) => _inner.GetWeaponType(skillId);
        public int GetAmmoType(ushort skillId) => _inner.GetAmmoType(skillId);
        public int GetAmmoQty(ushort skillId, ushort level) => _inner.GetAmmoQty(skillId, level);
        public int GetSplash(ushort skillId, ushort level) => _inner.GetSplash(skillId, level);
        public int GetUnitId(ushort skillId) => _inner.GetUnitId(skillId);
        public int GetUnitId2(ushort skillId) => _inner.GetUnitId2(skillId);
        public int GetUnitTarget(ushort skillId) => _inner.GetUnitTarget(skillId);
        public int GetUnitBlTarget(ushort skillId) => _inner.GetUnitBlTarget(skillId);
        public int GetUnitInterval(ushort skillId) => _inner.GetUnitInterval(skillId);
        public int GetUnitRange(ushort skillId) => _inner.GetUnitRange(skillId);
        public int GetUnitLayoutType(ushort skillId) => _inner.GetUnitLayoutType(skillId);
        public bool GetUnitFlag(ushort skillId, SkillUnitFlag flag) => _inner.GetUnitFlag(skillId, flag);
        public int GetElementalType(ushort skillId) => _inner.GetElementalType(skillId);
        public ushort Name2Id(string name) => _inner.Name2Id(name);
        public ushort Dummy2SkillId(ushort dummyId) => _inner.Dummy2SkillId(dummyId);
    }
}
