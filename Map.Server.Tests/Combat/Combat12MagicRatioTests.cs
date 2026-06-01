using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Skills;
using Map.Server.Skills.Behaviors;
using Map.Server.Skills.Behaviors.Mage;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Combat;

/// <summary>
/// COMBAT-12 — magic-skill damage now routes through the plugin's
/// <see cref="SkillImpl.CalculateSkillRatio"/> (rAthena
/// <c>battle_calc_attack_skill_ratio</c> is shared by BF_WEAPON and BF_MAGIC).
/// A magic plugin's ratio is the authority (replaces skill_db DamageRate, no
/// double-count); plugins that don't override the ratio keep the DamageRate
/// fallback.
/// </summary>
public class Combat12MagicRatioTests
{
    // ---- SoulStrike: +5*lv vs undead must actually scale bolt damage ----

    [Fact]
    public void SoulStrike_UndeadTarget_GetsPlus5PerLevel_NonUndeadDoesNot()
    {
        var ctx = Build(new SoulStrike());
        var src = ctx.Caster(matk: 100);

        var undead = ctx.Target(race: BattleRace.Undead);     // battle_check_undead → true
        var normal = ctx.Target(race: BattleRace.Formless);   // no bonus

        // Single bolt via the magic pipeline (SoulStrike emits N of these).
        var dUndead = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, undead, SkillIds.MG_SOULSTRIKE, 10);
        var dNormal = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, normal, SkillIds.MG_SOULSTRIKE, 10);

        // Both targets are DefenseElement.Neutral so the element rate cancels:
        // ratio 150 (100 + 5*10) vs 100 → undead deals exactly 1.5×.
        Assert.True(dNormal > 0);
        Assert.Equal(dNormal * 150 / 100, dUndead);
    }

    [Fact]
    public void SoulStrike_lv2_UndeadBonusIsPlus10Percent()
    {
        var ctx = Build(new SoulStrike());
        var src = ctx.Caster(matk: 100);
        var undead = ctx.Target(race: BattleRace.Undead);
        var normal = ctx.Target(race: BattleRace.Formless);

        var dUndead = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, undead, SkillIds.MG_SOULSTRIKE, 2);
        var dNormal = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, normal, SkillIds.MG_SOULSTRIKE, 2);

        Assert.Equal(dNormal * 110 / 100, dUndead); // 100 + 5*2
    }

    // ---- No double-count: a plugin ratio is applied once, not ×DamageRate ----

    [Fact]
    public void MagicPluginRatio_AppliedOnce_NotMultipliedByDamageRate()
    {
        var ctx = Build(new FixedRatioMagic(SkillIds.MG_FIREBOLT, ratio: 300));
        var src = ctx.Caster(matk: 100);
        var target = ctx.Target(race: BattleRace.Formless);

        var dealt = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, target, SkillIds.MG_FIREBOLT, 1);

        // Expected = the pipeline run with ratePerLevel=300 exactly once.
        var single = ctx.Battle.CalcMagicAttack(src, ctx.Target(race: BattleRace.Formless), SkillIds.MG_FIREBOLT, 1, 300).Damage;
        Assert.Equal(single, dealt);

        // And it must NOT be the DamageRate-double-counted value when FireBolt's
        // DamageRate differs from 100.
        var fireboltRate = RateAt(ctx.Db, SkillIds.MG_FIREBOLT, 1);
        if (fireboltRate != 100)
        {
            var doubled = ctx.Battle.CalcMagicAttack(src, ctx.Target(race: BattleRace.Formless),
                SkillIds.MG_FIREBOLT, 1, 300 * fireboltRate / 100).Damage;
            Assert.NotEqual(doubled, dealt);
        }
    }

    // ---- Override gate: a non-overriding plugin keeps the DamageRate fallback ----

    [Fact]
    public void MagicPluginWithoutRatioOverride_UsesDamageRateFallback()
    {
        var ctx = Build(new PlainMagic(SkillIds.MG_FIREBOLT));
        var src = ctx.Caster(matk: 100);
        var target = ctx.Target(race: BattleRace.Formless);

        var dealt = ctx.Skill.SkillAttack(BattleAttackType.Magic, src, src, target, SkillIds.MG_FIREBOLT, 1);

        var rate = RateAt(ctx.Db, SkillIds.MG_FIREBOLT, 1);
        var expected = ctx.Battle.CalcMagicAttack(src, ctx.Target(race: BattleRace.Formless),
            SkillIds.MG_FIREBOLT, 1, rate).Damage;
        Assert.Equal(expected, dealt);
    }

    // ---------- helpers ----------

    private static int RateAt(SkillDb db, ushort skillId, ushort lvl)
    {
        var def = db.Get(skillId);
        return def != null && def.DamageRate.Length > lvl ? def.DamageRate[lvl] : 100;
    }

    private sealed class FixedRatioMagic : SkillImpl
    {
        private readonly int _ratio;
        public FixedRatioMagic(ushort id, int ratio) : base(id) => _ratio = ratio;
        public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel) => _ratio;
    }

    private sealed class PlainMagic : SkillImpl
    {
        public PlainMagic(ushort id) : base(id) { }
        // No CalculateSkillRatio override → DamageRate fallback.
    }

    private static TestContext Build(SkillImpl plugin)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(), NullLogger<MovementService>.Instance);
        var ids = new EntityIdAllocator();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            new MobSpawnRegistry(), entities, world, new StubMobDb(), new EmptyItemCatalog(), itemDrops,
            movement, visibility, ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var battle = new BattleCalculator(new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities, battle, NullLogger<DamageService>.Instance);
        var db = new SkillDb();
        var behaviors = new SkillBehaviorRegistry(new[] { plugin });
        var skill = new SkillAttackService(db, battle, damage, entities,
            NullLogger<SkillAttackService>.Instance, sc: null,
            behaviors: new Lazy<SkillBehaviorRegistry>(() => behaviors));
        return new TestContext(skill, battle, db, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        ISkillAttackService Skill, IBattleCalculator Battle, SkillDb Db,
        EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public MobEntity Caster(int matk)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Caster", MapId, 50, 50) { Hp = 100000 };
            m.Level = 1;                       // <=99 → no RE_LVL_MDMOD
            m.Stats.MaxHp = 100000;
            m.Stats.MatkMin = (ushort)matk;
            m.Stats.MatkMax = (ushort)matk;
            m.Stats.WeaponElement = 0;         // Neutral → element rate cancels
            Entities.Add(m);
            return m;
        }

        public MobEntity Target(BattleRace race)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Target", MapId, 51, 50) { Hp = 100000 };
            m.Level = 1;
            m.Stats.MaxHp = 100000;
            m.Stats.Mdef = 0;
            m.Stats.Mdef2 = 0;
            m.Stats.DefenseElement = BattleElement.Neutral;
            m.Stats.ElementLevel = 1;
            m.Stats.Race = race;
            Entities.Add(m);
            return m;
        }
    }

    private sealed class StubMobDb : IMobDb
    {
        public int Count => 0;
        public MobDbEntry? Get(int classId) => null;
        public MobDbEntry? GetByAegisName(string n) => null;
        public IEnumerable<MobDbEntry> All() => Array.Empty<MobDbEntry>();
        public void Reload() { }
    }

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }

    private sealed class StubWorldRegistry : IMapWorldRegistry
    {
        private readonly Dictionary<string, MapData> _maps;
        public StubWorldRegistry(params MapData[] maps) =>
            _maps = maps.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        public MapData? Get(string name) => _maps.GetValueOrDefault(name);
        public IEnumerable<MapData> All => _maps.Values;
        public int TotalCells => _maps.Values.Sum(m => m.CellCount);
        public bool Contains(string name) => _maps.ContainsKey(name);
    }
}
