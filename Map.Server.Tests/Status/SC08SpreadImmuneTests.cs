using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Status.StatusOps;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-08 — SCF_SPREADEFFECT flags + Deadly Infect spread trigger + the
/// Hermode/DeadlyDefeasance bits of status_isimmune.
/// </summary>
public class SC08SpreadImmuneTests
{
    // ---- SpreadEffect flag table: exactly the 18 rAthena SCs ----

    [Fact]
    public void The18SpreadSCs_AreFlagged_OthersNot()
    {
        var reg = new StatusEffectRegistry();
        var spread = new[]
        {
            StatusType.Poison, StatusType.Curse, StatusType.Silence, StatusType.Confusion,
            StatusType.Blind, StatusType.Bleeding, StatusType.Hallucination, StatusType.Burning,
            StatusType.Freezing, StatusType.Toxin, StatusType.Paralyse, StatusType.Venombleed,
            StatusType.Magicmushroom, StatusType.Deathhurt, StatusType.Pyrexia,
            StatusType.Oblivioncurse, StatusType.Leechesend, StatusType.Bodypaint,
        };
        foreach (var t in spread)
            Assert.True((reg.GetEffectiveFlags(t) & ScfFlag.SpreadEffect) != 0, $"{t} should be SpreadEffect");

        // Not in the list — must NOT spread.
        foreach (var t in new[] { StatusType.DeadlyPoison, StatusType.Stun, StatusType.Blessing, StatusType.Freeze })
            Assert.True((reg.GetEffectiveFlags(t) & ScfFlag.SpreadEffect) == 0, $"{t} should NOT be SpreadEffect");
    }

    // ---- Spread(): flagged SCs propagate, others don't ----

    [Fact]
    public void Spread_PropagatesFlaggedSCs_NotBuffs()
    {
        var ctx = Build();
        var src = ctx.AddMob(100, 100);
        var tgt = ctx.AddMob(101, 100);
        // Poison + Bleeding are SpreadEffect DoTs with no LUK/MDEF landing gate
        // (Curse would be LUK-0-immune on a default mob); Blessing must NOT spread.
        ctx.Sc.Start(src, StatusType.Poison, 1, 0, 0, 0, durationMs: 30_000);
        ctx.Sc.Start(src, StatusType.Bleeding, 1, 0, 0, 0, durationMs: 30_000);
        ctx.Sc.Start(src, StatusType.Blessing, 5, 0, 0, 0, durationMs: 30_000);

        var n = ctx.Sc.Spread(src, tgt);

        Assert.NotNull(ctx.Sc.Get(tgt, StatusType.Poison));
        Assert.NotNull(ctx.Sc.Get(tgt, StatusType.Bleeding));
        Assert.Null(ctx.Sc.Get(tgt, StatusType.Blessing)); // buff, not SpreadEffect
        Assert.Equal(2, n);
    }

    // ---- Deadly Infect: melee hit propagates both directions ----

    [Fact]
    public void DeadlyInfect_OnHit_SpreadsAttackerDotsToTarget()
    {
        var ctx = Build(roll: 0); // 0 < 30+10*5 = 80 → spread fires
        var attacker = ctx.AddMob(100, 100);
        var victim = ctx.AddMob(101, 100); victim.Stats.MaxHp = 100000; victim.Hp = 100000;
        ctx.Sc.Start(attacker, StatusType.Deadlyinfect, 5, 0, 0, 0, durationMs: 30_000);
        ctx.Sc.Start(attacker, StatusType.Poison, 1, 0, 0, 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(victim, 100, attacker);

        Assert.NotNull(ctx.Sc.Get(victim, StatusType.Poison)); // spread attacker→victim
    }

    [Fact]
    public void DeadlyInfect_HighRoll_NoSpread()
    {
        var ctx = Build(roll: 99); // 99 < 80 false → no spread
        var attacker = ctx.AddMob(100, 100);
        var victim = ctx.AddMob(101, 100); victim.Stats.MaxHp = 100000; victim.Hp = 100000;
        ctx.Sc.Start(attacker, StatusType.Deadlyinfect, 5, 0, 0, 0, durationMs: 30_000);
        ctx.Sc.Start(attacker, StatusType.Poison, 1, 0, 0, 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(victim, 100, attacker);

        Assert.Null(ctx.Sc.Get(victim, StatusType.Poison));
    }

    // ---- IsImmune: Hermode / DeadlyDefeasance / mob mode ----

    [Fact]
    public void IsImmune_Hermode_True_DeadlyDefeasance_False_MobMode_True()
    {
        var ctx = Build();
        var ops = new StatusOpsService(ctx.Sc, NullLogger<StatusOpsService>.Instance);

        var hermoded = ctx.AddMob(100, 100);
        ctx.Sc.Start(hermoded, StatusType.Hermode, 1, 0, 0, 0, durationMs: 30_000);
        Assert.True(ops.IsImmune(hermoded));

        var defeased = ctx.AddMob(101, 100);
        defeased.Stats.Mode |= MobMode.StatusImmune;       // would be immune by mode...
        ctx.Sc.Start(defeased, StatusType.DeadlyDefeasance, 1, 0, 0, 0, durationMs: 30_000);
        Assert.False(ops.IsImmune(defeased));               // ...but DeadlyDefeasance strips it

        var boss = ctx.AddMob(102, 100);
        boss.Stats.Mode |= MobMode.StatusImmune;
        Assert.True(ops.IsImmune(boss));

        var normal = ctx.AddMob(103, 100);
        Assert.False(ops.IsImmune(normal));
    }

    // ---------- rig ----------

    private static TestContext Build(int roll = 0)
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
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance, rng: new FixedRandom(roll));
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        var damageWithSc = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            sc: new Lazy<IStatusChangeService>(() => sc), rng: new FixedRandom(roll));
        return new TestContext(damageWithSc, sc, entities, ids);
    }

    private sealed record TestContext(DamageService Damage, StatusChangeService Sc, EntityRegistry Entities, EntityIdAllocator Ids)
    {
        public MobEntity AddMob(short x, short y)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", (uint)"test_map".GetHashCode(), x, y) { Hp = 1000 };
            m.Stats.MaxHp = 1000;
            Entities.Add(m);
            return m;
        }
    }

    private sealed class FixedRandom : Random
    {
        private readonly int _v;
        public FixedRandom(int v) => _v = v;
        public override int Next(int maxValue) => _v;
        public override int Next(int minValue, int maxValue) => _v;
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
