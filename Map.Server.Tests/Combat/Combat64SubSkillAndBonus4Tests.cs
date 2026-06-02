using System;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Inventory.Script;
using Map.Server.Mob;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Spawn;
using Map.Server.Status;
using MobEntity = Map.Server.Entities.MobEntity;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-64 — the defender per-skill reduction (<c>bonus2 bSubSkill</c> /
/// <c>pc_sub_skillatk_bonus</c>, battle.cpp:7873) symmetric to the offensive
/// <c>bSkillAtk</c>, plus the <c>bonus4 bAddEff</c> AddEff family with an
/// explicit duration (rounding out the bonus3/bonus4 AddEff coverage on the
/// live <see cref="ScriptedBonusHost"/> path; the regex extractor was retired
/// in CONV-5).
/// </summary>
public class Combat64SubSkillAndBonus4Tests
{
    // ---- bSubSkill parse (shared ApplyIndexedBonus, used by the live bonus2 host) ----

    [Fact]
    public void Extractor_parses_bSubSkill_by_name()
    {
        var b = new EquipBonusBundle();
        BonusScriptExtractor.Apply("bonus2 bSubSkill,SM_BASH,25;", b);
        Assert.Equal(25, b.SubSkillAtk.GetValueOrDefault(SkillIds.SM_BASH));
    }

    // ---- defender reduction: weapon-skill path (SkillImpl.ComputeSkillDamage) ----

    [Fact]
    public void Weapon_subskill_reduces_only_the_named_skill()
    {
        var plugin = new TestWeaponSkill(SkillIds.SM_BASH);
        var swing = new BattleDamage { Damage = 100, Lane = BattleAttackType.Weapon };

        // Defender carries a 25% reduction vs SM_BASH → ratio 100 → 100 → -25% → 75.
        var defender = new PlayerEntity(9, 9, "Tank", Guid.NewGuid(), 0, 0, 0);
        defender.EquipBonuses.SubSkillAtk[SkillIds.SM_BASH] = 25;
        Assert.Equal(75, plugin.ComputeSkillDamage(swing, NewAttacker(), defender, 1));

        // A mob defender (no cards) is unaffected.
        Assert.Equal(100, plugin.ComputeSkillDamage(swing, NewAttacker(), MakeMob(), 1));

        // A defender carded for a different skill is unaffected.
        var other = new PlayerEntity(8, 8, "Other", Guid.NewGuid(), 0, 0, 0);
        other.EquipBonuses.SubSkillAtk[SkillIds.MG_FIREBOLT] = 25;
        Assert.Equal(100, plugin.ComputeSkillDamage(swing, NewAttacker(), other, 1));
    }

    // ---- defender reduction: magic path (CalcMagicAttack) ----

    [Fact]
    public void Magic_subskill_reduces_only_the_named_skill()
    {
        var calc = new BattleCalculator(new Random(0));
        var caster = NewMage(matk: 200);
        var defender = new PlayerEntity(9, 9, "Tank", Guid.NewGuid(), 0, 0, 0);
        defender.EquipBonuses.SubSkillAtk[SkillIds.MG_FIREBOLT] = 20;

        // Fire Bolt → -20% (200 → 160); Cold Bolt (not carded) → unchanged.
        Assert.Equal(160, calc.CalcMagicAttack(caster, defender, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100).Damage);
        Assert.Equal(200, calc.CalcMagicAttack(caster, defender, SkillIds.MG_COLDBOLT, 5, ratePerLevel: 100).Damage);
    }

    [Fact]
    public void Offensive_and_defensive_skillatk_stack()
    {
        var calc = new BattleCalculator(new Random(0));
        var caster = NewMage(matk: 200);
        caster.EquipBonuses.SkillAtk[SkillIds.MG_FIREBOLT] = 50;     // +50% offensive
        var defender = new PlayerEntity(9, 9, "Tank", Guid.NewGuid(), 0, 0, 0);
        defender.EquipBonuses.SubSkillAtk[SkillIds.MG_FIREBOLT] = 25; // -25% defensive

        // 200 → +50% (300) → -25% (225).
        Assert.Equal(225, calc.CalcMagicAttack(caster, defender, SkillIds.MG_FIREBOLT, 5, ratePerLevel: 100).Damage);
    }

    // ---- bonus4 bAddEff: explicit duration on the AddEff family ----

    [Fact]
    public void Bonus4_addeff_records_an_onattack_proc_with_duration()
    {
        var bundle = new EquipBonusBundle();
        var pc = new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0);
        var host = new ScriptedBonusHost(pc, bundle);

        // bonus4 bAddEff, eff, rate, atfFlag, durMs — the 4th arg (atf) is dropped.
        host.bonus4("bAddEff", (int)StatusType.Stun, 500, 0, 3000);

        var entry = Assert.Single(bundle.AddEffOnAttack);
        Assert.Equal(StatusType.Stun, entry.Sc);
        Assert.Equal((short)500, entry.RatePermille);
        Assert.Equal(3000u, entry.DurationMs);
    }

    [Fact]
    public void Bonus4_addeffwhenhit_records_a_whenhit_proc()
    {
        var bundle = new EquipBonusBundle();
        var host = new ScriptedBonusHost(new PlayerEntity(1, 1, "Hero", Guid.NewGuid(), 0, 0, 0), bundle);
        host.bonus4("bAddEffWhenHit", (int)StatusType.Stun, 300, 0, 2000);

        var entry = Assert.Single(bundle.AddEffWhenHit);
        Assert.Equal(2000u, entry.DurationMs);
    }

    // ---- helpers ----

    private sealed class TestWeaponSkill : WeaponSkillImpl
    {
        public TestWeaponSkill(ushort id) : base(id) { }
    }

    private static PlayerEntity NewAttacker() => new(2, 2, "Atk", Guid.NewGuid(), 0, 0, 0);

    private static PlayerEntity NewMage(int matk)
    {
        var p = new PlayerEntity(1, 1, "Mage", Guid.NewGuid(), 0, 0, 0);
        p.Stats.MatkMin = p.Stats.MatkMax = (ushort)matk;
        return p;
    }

    private static MobEntity MakeMob()
    {
        var db = new MobDbEntry { Id = 1002, AegisName = "PORING", Name = "Poring", Hp = 5000 };
        var origin = new MobSpawnEntry { MapId = 0, MobClassId = 1002 };
        var m = new MobEntity(new EntityId(99), db, origin, mapId: 0, x: 0, y: 0);
        m.Stats.DefenseElement = BattleElement.Neutral; m.Stats.ElementLevel = 1;
        m.Stats.Mdef = 0; m.Stats.Mdef2 = 0; m.Stats.Def = 0; m.Stats.Def2 = 0;
        return m;
    }
}
