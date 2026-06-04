using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Spawn;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Tests.Warps;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Mob;

public class MobAiServiceTests
{
    [Fact]
    public void Aggressive_Mob_Locks_Nearest_Pc()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var farPc = ctx.AddPlayer(55, 50, 1);
        var closePc = ctx.AddPlayer(52, 50, 2);

        ctx.Ai.Tick(0);

        Assert.NotNull(mob.Attack);
        // The closer PC is at dist 2 vs 5 — should be picked.
        Assert.Equal(closePc.Id, mob.Attack!.TargetId);
    }

    // ---- AI-BOSS-ACTIVE-HP: mob_active_time / boss_active_time ----

    [Fact]
    public void Mob_stays_on_the_hard_path_briefly_after_the_pc_leaves_view()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        ctx.AddPlayer(52, 50, 1); // in view

        ctx.Ai.Tick(200);         // PC in view → records last_pcneartime=200, runs hard
        Assert.NotNull(mob.Attack); // acquired a target → was on the hard path

        // PC has now left view: within mob_active_time (5000 ms) the mob keeps running hard…
        Assert.False(ctx.Ai.ShouldRunLazy(mob, pcInView: false, nowTick: 1000));
        // …and only after the window elapses does it drop to lazy.
        Assert.True(ctx.Ai.ShouldRunLazy(mob, pcInView: false, nowTick: 200 + 5001));
        // A PC back in view is always hard.
        Assert.False(ctx.Ai.ShouldRunLazy(mob, pcInView: true, nowTick: 9_999_999));
    }

    [Fact]
    public void Mob_with_no_recent_pc_contact_goes_lazy_at_once()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        // never had a PC in view → no last_pcneartime → lazy immediately.
        Assert.True(ctx.Ai.ShouldRunLazy(mob, pcInView: false, nowTick: 1000));
    }

    [Fact]
    public void Boss_mob_uses_the_boss_active_window()
    {
        var ctx = Build();
        var boss = ctx.AddBossMob(50, 50);
        ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(200); // PC in view → records last_pcneartime for the boss
        // The MD_STATUSIMMUNE branch keeps the boss active within boss_active_time.
        Assert.False(ctx.Ai.ShouldRunLazy(boss, pcInView: false, nowTick: 1000));
        Assert.True(ctx.Ai.ShouldRunLazy(boss, pcInView: false, nowTick: 200 + 5001));
    }

    // ---- MOBAI-04: line-of-sight gate on the aggressive scan ----

    [Fact]
    public void Wall_blocked_pc_is_not_aggroed()
    {
        var path = new StubPath(losClear: false); // every line blocked
        var ctx = Build(path);
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        ctx.AddPlayer(52, 50, 1); // in view (dist 2) but LOS-blocked

        ctx.Ai.Tick(0);

        Assert.Null(mob.Attack); // wall between → no aggro through it
    }

    [Fact]
    public void In_los_pc_is_aggroed()
    {
        var path = new StubPath(losClear: true);
        var ctx = Build(path);
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(0);

        Assert.NotNull(mob.Attack);
        Assert.Equal(pc.Id, mob.Attack!.TargetId);
    }

    [Fact]
    public void Closest_in_los_wins_over_a_nearer_wall_blocked_pc()
    {
        var nearer = ((short)52, (short)50);
        // LOS clear for everyone EXCEPT the nearer PC's cell (it's behind a wall).
        var path = new StubPath((x1, y1) => !(x1 == nearer.Item1 && y1 == nearer.Item2));
        var ctx = Build(path);
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        ctx.AddPlayer(nearer.Item1, nearer.Item2, 1); // dist 2, wall-blocked
        var fartherClear = ctx.AddPlayer(55, 50, 2);   // dist 5, LOS-clear

        ctx.Ai.Tick(0);

        Assert.NotNull(mob.Attack);
        Assert.Equal(fartherClear.Id, mob.Attack!.TargetId); // reachable target chosen
    }

    [Fact]
    public void Null_path_service_falls_back_to_distance_only()
    {
        // No IPathService (the large existing test suite path) → LOS treated as clear.
        var ctx = Build(paths: null);
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(0);

        Assert.NotNull(mob.Attack);
        Assert.Equal(pc.Id, mob.Attack!.TargetId);
    }

    // ---- MOBAI-01: slave→master coupling ----

    [Fact]
    public void Slave_follows_master_when_out_of_slave_distance()
    {
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        var slave = ctx.AddSlaveMob(56, 50, master, aggressive: false); // dist 6 > MOB_SLAVEDISTANCE

        ctx.Ai.Tick(0);

        Assert.Equal(6, slave.MasterDist);
        Assert.NotNull(slave.Walk);   // walking toward the master
        Assert.Null(slave.Attack);    // pure follow — not engaging
    }

    [Fact]
    public void Idle_slave_inherits_master_target_after_the_link_throttle()
    {
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(80, 80, 1);      // far — out of the slave's small view, so only
        master.TargetId = (int)pc.Id.Value;      // inheritance (not the active scan) can set it
        var slave = ctx.AddSlaveMob(51, 50, master, aggressive: false, range2: 3); // adjacent

        // Before MIN_MOBLINKTIME (300ms): no inheritance.
        ctx.Ai.Tick(100);
        Assert.Equal(0, slave.TargetId);

        // After 300ms: inherit the master's target and engage it.
        ctx.Ai.Tick(400);
        Assert.Equal((int)pc.Id.Value, slave.TargetId);
        Assert.NotNull(slave.Attack);
        Assert.Equal(pc.Id, slave.Attack!.TargetId);
    }

    [Fact]
    public void Idle_slave_inherits_a_non_pc_master_target()
    {
        // MOBAI-06 — rAthena mob_ai_sub_hard_slavemob inherits the master's target whatever its type,
        // so a slave joins the master against a MOB target (e.g. a player-summoned slave helping its
        // master attack a monster) — not only a mob-master's slaves piling onto a PC.
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        var enemyMob = ctx.AddPassiveMob(80, 80);   // a MOB the master is engaging, far from the slave
        master.TargetId = (int)enemyMob.Id.Value;
        var slave = ctx.AddSlaveMob(51, 50, master, aggressive: false, range2: 3); // adjacent

        ctx.Ai.Tick(400); // past MIN_MOBLINKTIME

        Assert.Equal((int)enemyMob.Id.Value, slave.TargetId); // inherited the non-PC target (was PC-only before)
    }

    [Fact]
    public void Slave_dies_when_its_master_is_gone()
    {
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        var slave = ctx.AddSlaveMob(52, 50, master, aggressive: false);
        ctx.Entities.Remove(master.Id); // master gone

        ctx.Ai.Tick(0);

        Assert.Equal(0, slave.Hp); // status_kill (no IMobDeathSink in this harness → HP→0)
    }

    [Fact]
    public void Non_aggressive_slave_that_lost_its_target_still_scans_for_aggro()
    {
        // slave_lost_target forces the active scan even for a non-aggressive mob, so the slave
        // joins its master's fight. Master idle (no target to inherit) + a PC in the slave's view.
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        var slave = ctx.AddSlaveMob(51, 50, master, aggressive: false, range2: 5); // adjacent to master
        var pc = ctx.AddPlayer(53, 50, 1); // in the slave's view (dist 2)

        ctx.Ai.Tick(400); // throttle elapsed; master has no target → inherit fails → slave_lost_target

        Assert.NotNull(slave.Attack);          // aggroed via the forced scan
        Assert.Equal(pc.Id, slave.Attack!.TargetId);
    }

    [Fact]
    public void Non_slave_non_aggressive_mob_does_not_aggro()
    {
        // Control for the slave_lost_target override: a passive mob with no master never scans.
        var ctx = Build();
        var mob = ctx.AddPassiveMob(50, 50);
        ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(0);

        Assert.Null(mob.Attack);
    }

    [Fact]
    public void Slave_death_lowers_the_masters_live_slave_count()
    {
        // The replenish precondition: a slave dying (Hp 0) drops CountSlaves, which re-fires the
        // master's NPC_SUMMONSLAVE via the SlaveLessThan skill condition (unchanged by this ticket).
        var ctx = Build();
        var master = ctx.AddAggressiveMob(50, 50, range2: 10);
        ctx.AddSlaveMob(51, 50, master, aggressive: false);
        var dying = ctx.AddSlaveMob(52, 50, master, aggressive: false);
        ctx.AddSlaveMob(53, 50, master, aggressive: false);

        var slaves = new Map.Server.Mob.Slaves.SlaveMobService(ctx.Entities);
        Assert.Equal(3, slaves.CountSlaves(master));

        dying.Hp = 0; // a slave dies
        Assert.Equal(2, slaves.CountSlaves(master)); // count drops → summon condition re-fires
    }

    [Fact]
    public void Passive_Mob_Does_Not_Engage()
    {
        var ctx = Build();
        var mob = ctx.AddPassiveMob(50, 50);
        ctx.AddPlayer(51, 50, 1);

        ctx.Ai.Tick(0);

        Assert.Null(mob.Attack);
    }

    [Fact]
    public void OutOfRange_Pc_Ignored()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 5);
        ctx.AddPlayer(80, 50, 1);

        ctx.Ai.Tick(0);

        Assert.Null(mob.Attack);
    }

    [Fact]
    public void Opt1Stone_DropsTargetAndRefusesEngage()
    {
        // T5.1d — rAthena mob.cpp:1864 OPT1 gate. Stone-petrified mob
        // must drop its target and not re-engage on the next tick.
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(52, 50, 1);

        // First tick — engages normally (no SC yet).
        ctx.Ai.Tick(0);
        Assert.NotNull(mob.Attack);

        // Apply Stone (OPT1_STONE in rAthena), tick after the throttle
        // expires.
        ctx.Sc.Start(mob, StatusType.Stone, 1, 0, 0, 0,
            durationMs: 5_000, source: null, nowTick: 200);
        ctx.Ai.Tick(200);

        Assert.Null(mob.Attack);
        Assert.Equal(0, mob.TargetId);
        Assert.Equal(0, mob.AttackedId);
    }

    [Fact]
    public void Opt1Burning_KeepsTarget()
    {
        // Burning is the OPT1 exception (mob.cpp:1864 — explicitly
        // excluded from the lose-target check). Mob should keep
        // engaging through it.
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(0);
        Assert.NotNull(mob.Attack);

        ctx.Sc.Start(mob, StatusType.Burning, 1, 0, 0, 0,
            durationMs: 5_000, source: null, nowTick: 200);
        ctx.Ai.Tick(200);

        Assert.NotNull(mob.Attack); // still engaged
    }

    [Fact]
    public void Throttle_BlocksRepeatedTicksUntilMinThinkTime()
    {
        var ctx = Build();
        var mob = ctx.AddAggressiveMob(50, 50, range2: 10);
        var pc = ctx.AddPlayer(52, 50, 1);

        ctx.Ai.Tick(nowTick: 0);
        Assert.NotNull(mob.Attack);
        // Manually clear target as if a kill happened, then re-tick within the
        // MIN_MOBTHINKTIME (100ms) — should NOT re-acquire.
        mob.Attack = null;
        ctx.Ai.Tick(nowTick: 50);
        Assert.Null(mob.Attack);

        // After throttle elapses, AI re-runs and re-engages.
        ctx.Ai.Tick(nowTick: 200);
        Assert.NotNull(mob.Attack);
    }

    private static TestContext Build(Map.Server.Pathing.IPathService? paths = null)
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var spawnRegistry = new MobSpawnRegistry();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(
            spawnRegistry, entities, world, mobDb, itemCatalog, itemDrops, movement, visibility,
            ids, new StatusCalcService(), NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        var sc = new StatusChangeService(damage, entities,
            new StatusEffectRegistry(), NullLogger<StatusChangeService>.Instance);
        var ai = new MobAiService(entities, attack, NullLogger<MobAiService>.Instance, movement: movement, sc: sc, paths: paths);
        return new TestContext(ai, entities, ids, sc, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        MobAiService Ai,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
        StatusChangeService Sc,
        uint MapId)
    {
        public PlayerEntity AddPlayer(short x, short y, int charId)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }

        public MobEntity AddAggressiveMob(short x, short y, int range2)
        {
            var db = new MobDbEntry
            {
                Id = 1031, AegisName = "POPORING", Name = "Poporing",
                Hp = 500, ChaseRange = range2, AttackRange = 1,
                Modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Aggressive"] = true,
                    ["CanAttack"] = true,
                    ["CanMove"] = true,
                },
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1031 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }

        public MobEntity AddPassiveMob(short x, short y)
        {
            var db = new MobDbEntry
            {
                Id = 1002, AegisName = "PORING", Name = "Poring",
                Hp = 50, ChaseRange = 10, AttackRange = 1,
                // Default modes — no Aggressive flag set.
                Modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["CanMove"] = true,
                },
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1002 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }

        // AI-BOSS-ACTIVE-HP — an MVP/boss mob (MD_STATUSIMMUNE) for the boss-active-window test.
        public MobEntity AddBossMob(short x, short y)
        {
            var db = new MobDbEntry
            {
                Id = 1373, AegisName = "LORD_OF_DEATH", Name = "Lord of Death",
                Hp = 3_000_000, ChaseRange = 12, AttackRange = 2,
                Modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Aggressive"] = true, ["CanAttack"] = true, ["CanMove"] = true,
                    ["StatusImmune"] = true, ["Mvp"] = true,
                },
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1373 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            Entities.Add(mob);
            return mob;
        }

        // MOBAI-01 — a mob slave owned by `master`. CanMove always; aggressive optional (a
        // non-aggressive slave only scans for aggro when slave_lost_target is set).
        public MobEntity AddSlaveMob(short x, short y, Entity master, bool aggressive, int range2 = 3)
        {
            var modes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["CanAttack"] = true,
                ["CanMove"] = true,
            };
            if (aggressive) modes["Aggressive"] = true;
            var db = new MobDbEntry
            {
                Id = 1109, AegisName = "DEVIRUCHI", Name = "Deviruchi",
                Hp = 300, ChaseRange = range2, AttackRange = 1, Modes = modes,
            };
            var origin = new MobSpawnEntry { MapId = MapId, MobClassId = 1109 };
            var mob = new MobEntity(Ids.NextMob(), db, origin, MapId, x, y);
            new StatusCalcService().CalcMob(mob);
            mob.MasterId = master.Id;
            Entities.Add(mob);
            return mob;
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

    private sealed class EmptyItemCatalog : IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }

    // MOBAI-04 — stub IPathService whose PathSearchLong (the LOS check) is driven by a predicate
    // over the destination cell. Only PathSearchLong is exercised by the aggressive scan.
    private sealed class StubPath : Map.Server.Pathing.IPathService
    {
        private readonly Func<short, short, bool> _los;
        public StubPath(bool losClear) => _los = (_, _) => losClear;
        public StubPath(Func<short, short, bool> los) => _los = los;

        public bool PathSearchLong(uint mapId, short x0, short y0, short x1, short y1) => _los(x1, y1);

        public int Distance(short x0, short y0, short x1, short y1) => Math.Max(Math.Abs(x0 - x1), Math.Abs(y0 - y1));
        public int DistanceClient(short x0, short y0, short x1, short y1) => Distance(x0, y0, x1, y1);
        public bool CheckDistance(short x0, short y0, short x1, short y1, int range) => Distance(x0, y0, x1, y1) <= range;
        public bool CheckDistanceClient(short x0, short y0, short x1, short y1, int range) => CheckDistance(x0, y0, x1, y1, range);
        public bool DirectionDiagonal(int dir) => false;
        public int DirectionOpposite(int dir) => dir;
        public bool PathSearch(uint mapId, short x0, short y0, short x1, short y1, byte flag) => true;
        public (short x, short y) BlownPos(uint mapId, short x, short y, int direction, int count) => (x, y);
    }
}
