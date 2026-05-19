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

    private static TestContext Build()
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
        var ai = new MobAiService(entities, attack, NullLogger<MobAiService>.Instance);
        return new TestContext(ai, entities, ids, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        MobAiService Ai,
        EntityRegistry Entities,
        EntityIdAllocator Ids,
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
}
