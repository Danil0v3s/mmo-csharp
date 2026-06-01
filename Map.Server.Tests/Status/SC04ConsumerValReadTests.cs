using System;
using System.Collections.Generic;
using System.Linq;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Session;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Status;

/// <summary>
/// SC-04 — combat-consumer Val reads that previously had a write side but no
/// reader: Kaupe (dodge-next-hit), Kaahi (heal-on-hit), Richmankim (EXP %).
/// </summary>
public class SC04ConsumerValReadTests
{
    // ---- Kaupe: roll Val2% to fully block a hit; Val3 = block count ----

    [Fact]
    public void Kaupe_BlocksHit_WhenRollUnderChance_AndDecrementsCount()
    {
        var ctx = Build(roll: 0); // 0 < 33 → block
        var mob = ctx.AddMob(100, 100); mob.Stats.MaxHp = 1000; mob.Hp = 1000;
        ctx.Sc.Start(mob, StatusType.Kaupe, val1: 1, 0, 0, 0, durationMs: 30_000); // Val2=33, Val3=1

        ctx.Damage.ApplyDamage(mob, 500);

        Assert.Equal(1000, mob.Hp);                          // fully blocked
        Assert.Null(ctx.Sc.Get(mob, StatusType.Kaupe));      // Val3 0 → SC ended
    }

    [Fact]
    public void Kaupe_NoBlock_WhenRollAboveChance()
    {
        var ctx = Build(roll: 99); // 99 < 33 false → no block
        var mob = ctx.AddMob(100, 100); mob.Stats.MaxHp = 1000; mob.Hp = 1000;
        ctx.Sc.Start(mob, StatusType.Kaupe, val1: 1, 0, 0, 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 500);

        Assert.Equal(500, mob.Hp);
        Assert.NotNull(ctx.Sc.Get(mob, StatusType.Kaupe));   // still active
    }

    // ---- Kaahi: on-hit heal Val2 HP, costs Val3 SP ----

    [Fact]
    public void Kaahi_HealsOnHit_AndChargesSp()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100); mob.Stats.MaxHp = 1000; mob.Hp = 500; mob.Stats.Sp = 100;
        var src = ctx.AddMob(101, 100);
        ctx.Sc.Start(mob, StatusType.Kaahi, val1: 5, 0, 0, 0, durationMs: 30_000); // Val2=1000 heal, Val3=25 SP

        ctx.Damage.ApplyDamage(mob, 200, src);

        // 500 - 200 = 300, then Kaahi heals min(700, 1000) = 700 → 1000; SP 100-25=75.
        Assert.Equal(1000, mob.Hp);
        Assert.Equal(75, mob.Stats.Sp);
    }

    [Fact]
    public void Kaahi_NoHeal_WhenSpInsufficient()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100); mob.Stats.MaxHp = 1000; mob.Hp = 500; mob.Stats.Sp = 10; // < 25
        var src = ctx.AddMob(101, 100);
        ctx.Sc.Start(mob, StatusType.Kaahi, val1: 5, 0, 0, 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 200, src);

        Assert.Equal(300, mob.Hp); // damage applied, no heal
        Assert.Equal(10, mob.Stats.Sp);
    }

    [Fact]
    public void Kaahi_DoesNotReviveLethalHit()
    {
        var ctx = Build();
        var mob = ctx.AddMob(100, 100); mob.Stats.MaxHp = 1000; mob.Hp = 100; mob.Stats.Sp = 100;
        var src = ctx.AddMob(101, 100);
        ctx.Sc.Start(mob, StatusType.Kaahi, val1: 5, 0, 0, 0, durationMs: 30_000);

        ctx.Damage.ApplyDamage(mob, 999, src); // lethal

        Assert.Equal(0, mob.Hp); // dead, not healed back up
    }

    // ---- Richmankim: +Val2% mob-kill EXP ----

    [Fact]
    public void Richmankim_BoostsMobKillExp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(100, 100); pc.Level = 1; pc.BaseExp = 0; pc.Hp = 100;
        ctx.Sc.Start(pc, StatusType.Richmankim, val1: 5, 0, 0, 0, durationMs: 60_000); // Val2 = 10+10*5 = 60

        // baseExp 5 → +60% = 8 (still under NextBaseExp(1)=9, so no level-up).
        ctx.Exp.GainExp(pc, baseExp: 5, jobExp: 0, mobLevel: 50);

        Assert.Equal(8, pc.BaseExp);
    }

    [Fact]
    public void Richmankim_DoesNotBoostNonMobExp()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(100, 100); pc.Level = 1; pc.BaseExp = 0; pc.Hp = 100;
        ctx.Sc.Start(pc, StatusType.Richmankim, val1: 5, 0, 0, 0, durationMs: 60_000);

        // No mobLevel → quest/GM/scroll EXP, not boosted (matches rAthena gate).
        ctx.Exp.GainExp(pc, baseExp: 5, jobExp: 0, mobLevel: null);

        Assert.Equal(5, pc.BaseExp);
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
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            rng: new FixedRandom(roll));
        var sc = new StatusChangeService(damage, entities, new StatusEffectRegistry(),
            NullLogger<StatusChangeService>.Instance);
        // The SC engine needs a back-reference so Get works through the same instance.
        var damageWithSc = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance,
            sc: sc, rng: new FixedRandom(roll));
        var exp = new ExpService(new StatusCalcService(), new NoSessions(),
            NullLogger<ExpService>.Instance, levelPenalty: null, sc: sc);
        return new TestContext(damageWithSc, sc, exp, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        DamageService Damage, StatusChangeService Sc, ExpService Exp,
        EntityRegistry Entities, EntityIdAllocator Ids, uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y)
        {
            var pc = new PlayerEntity(1, 1, "P", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddMob(short x, short y)
        {
            var m = new MobEntity(Ids.NextMob(), 1002, "Poring", MapId, x, y) { Hp = 1000 };
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

    private sealed class NoSessions : ISessionManagerAccessor
    {
        public Map.Server.MapSessionData? GetByEntityId(EntityId entityId) => null;
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
