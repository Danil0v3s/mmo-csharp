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
using Map.Server.Skills.Resolvers;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Skills;

/// <summary>
/// SKILL-05 — a plugin-backed weapon skill must yield ONE ratio regardless of
/// which dispatch path resolves it. The plugin's CalculateSkillRatio (via the
/// shared ComputeSkillDamage) is the single authority; SkillDefinition.DamageRate
/// is the no-plugin fallback only.
/// </summary>
public class SkillRatioConsistencyTests
{
    // A weapon plugin with a distinctive ratio (777%) that differs from every
    // DamageRate column value, so "plugin wins" is observable.
    private sealed class Ratio777 : WeaponSkillImpl
    {
        public Ratio777(ushort id) : base(id) { }
        public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel) => 777;
    }

    [Fact]
    public void PluginSkillSameRatioBothPaths()
    {
        var ctx = Build(plugin: new Ratio777(SkillIds.SM_BASH));
        var src = ctx.Attacker(100, 100);
        var target = ctx.Victim(101, 100);

        // Deterministic swing (no variance, guaranteed hit) so both reads match.
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        Assert.True(swing.Total > 0);

        // Canonical (plugin) value via the single entry point.
        var plugin = (WeaponSkillImpl)ctx.Behaviors.Get(SkillIds.SM_BASH)!;
        var expected = plugin.ComputeSkillDamage(swing, src, target, 1);

        // The funnel must produce the identical number...
        var funnel = ctx.SkillAttack.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillIds.SM_BASH, 1);
        Assert.Equal(expected, funnel);

        // ...and it must be the plugin ratio (777%), NOT the DamageRate column (130% @ lv1).
        Assert.Equal(swing.Total * 777 / 100, funnel);
        Assert.NotEqual(swing.Total * 130 / 100, funnel);
    }

    [Fact]
    public void NoPluginUsesDamageRate()
    {
        var ctx = Build(plugin: null); // empty registry → SM_BASH has no plugin
        var src = ctx.Attacker(100, 100);
        var target = ctx.Victim(101, 100);

        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var funnel = ctx.SkillAttack.SkillAttack(BattleAttackType.Weapon, src, src, target, SkillIds.SM_BASH, 1);

        // SM_BASH DamageRate[1] = 130 (fallback ratio source for a no-plugin skill).
        Assert.Equal(swing.Total * 130 / 100, funnel);
    }

    [Fact]
    public void Resolver_DefersToPlugin_WhenDispatchLeaks()
    {
        // If a plugin skill ever reaches the generic resolver, the resolver must
        // honor the plugin ratio (not DamageRate) so the two paths can't diverge.
        var ctx = Build(plugin: new Ratio777(SkillIds.SM_BASH));
        var src = ctx.Attacker(100, 100);
        var target = ctx.Victim(101, 100);
        var startHp = target.Hp;

        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        var def = ctx.Db.Get(SkillIds.SM_BASH)!;

        var resolver = new WeaponSkillResolver(ctx.Battle, ctx.Damage, ctx.Behaviors,
            NullLogger<WeaponSkillResolver>.Instance);
        resolver.Resolve(src, target, def, 1);

        var dealt = startHp - target.Hp;
        // Plugin ratio (777%), not DamageRate (130%).
        Assert.Equal(swing.Total * 777 / 100, dealt);
        Assert.NotEqual(swing.Total * 130 / 100, dealt);
    }

    // ---------- rig ----------

    private static TestContext Build(WeaponSkillImpl? plugin)
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
        var battle = new BattleCalculator(new FixedRandom()); // deterministic swing across calls
        var damage = new DamageService(visibility, mobSpawn, entities, battle, NullLogger<DamageService>.Instance);
        var db = new SkillDb();
        var behaviors = new SkillBehaviorRegistry(plugin is null ? Array.Empty<SkillImpl>() : new SkillImpl[] { plugin });
        var skillAttack = new SkillAttackService(db, battle, damage, entities,
            NullLogger<SkillAttackService>.Instance, sc: null, behaviors: behaviors);
        return new TestContext(skillAttack, behaviors, battle, damage, db, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        ISkillAttackService SkillAttack, SkillBehaviorRegistry Behaviors, IBattleCalculator Battle,
        IDamageService Damage, ISkillDb Db, EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity Attacker(short x, short y)
        {
            var pc = new PlayerEntity(1, 1, "Atk", Guid.NewGuid(), MapId, x, y);
            // Deterministic, guaranteed-hit, no-crit swing.
            pc.Stats.WatkMin = pc.Stats.WatkMax = 100;
            pc.Stats.Batk = 50;
            pc.Stats.Hit = 10000;
            pc.Stats.Cri = 0;
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity Victim(short x, short y)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y);
            m.Stats.MaxHp = 100000; m.Hp = 100000;
            m.Stats.Def = 0; m.Stats.Def2 = 0; m.Stats.Flee = 0;
            Entities.Add(m);
            return m;
        }
    }

    // Deterministic RNG so CalcWeaponAttack returns the identical swing on every
    // call (no weapon-variance / hit / crit jitter between the external read and
    // the funnel's internal read).
    private sealed class FixedRandom : Random
    {
        public override int Next() => 0;
        public override int Next(int maxValue) => 0;
        public override int Next(int minValue, int maxValue) => minValue;
        public override double NextDouble() => 0.0;
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
