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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Movement;

public class PcSetposServiceTests
{
    [Fact]
    public void Setpos_SameMap_MovesEntity_AndCancelsAttack()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);
        pc.Attack = new AttackState { TargetId = new EntityId(999), Continuous = true };

        var result = ctx.Setpos.Setpos(pc, "test_map", 50, 50);

        Assert.Equal(SetposResult.Ok, result);
        Assert.Equal(50, pc.X);
        Assert.Equal(50, pc.Y);
        Assert.Null(pc.Attack);
    }

    [Fact]
    public void Setpos_UnknownMap_Rejected()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        Assert.Equal(SetposResult.UnknownMap,
            ctx.Setpos.Setpos(pc, "nonexistent_map", 50, 50));
    }

    [Fact]
    public void Setpos_OffMap_Rejected()
    {
        var ctx = Build();
        var pc = ctx.AddPlayer(1, 100, 100);

        Assert.Equal(SetposResult.OffMap,
            ctx.Setpos.Setpos(pc, "test_map", -1, 50));
    }

    // Nearby-walkable-cell fallback when the target tile is a wall is
    // exercised end-to-end by the live replay test; we don't reach into
    // MapData's private cell bytes here.

    private static TestContext Build()
    {
        const string mapName = "test_map";
        var bytes = new byte[200 * 200]; // all 0 = walkable
        var map = new MapData(mapName, 200, 200, bytes);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(entities, dispatcher);
        var movement = new MovementService(entities, world, visibility,
            new NoOpWarpService(), new NoOpWarpDispatcher(),
            NullLogger<MovementService>.Instance);
        var mobDb = new StubMobDb();
        var ids = new EntityIdAllocator();
        var itemCatalog = new EmptyItemCatalog();
        var itemDrops = new ItemDropService(entities, ids, visibility, NullLogger<ItemDropService>.Instance);
        var mobSpawn = new MobSpawnService(new MobSpawnRegistry(), entities, world, mobDb,
            itemCatalog, itemDrops, movement, visibility, ids, new StatusCalcService(),
            NullLogger<MobSpawnService>.Instance, new Random(0));
        var damage = new DamageService(visibility, mobSpawn, entities,
            new BattleCalculator(new Random(0)), NullLogger<DamageService>.Instance);
        var attack = new AttackService(entities, damage, movement, NullLogger<AttackService>.Instance);
        var sessions = new NoSessions();
        var sp = new ServiceCollection()
            .AddSingleton<IAttackStopper>(attack)
            .BuildServiceProvider();
        var setpos = new PcSetposService(world, entities, visibility, sp, sessions,
            NullLogger<PcSetposService>.Instance);
        return new TestContext(setpos, entities, map, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        PcSetposService Setpos,
        EntityRegistry Entities,
        MapData Map,
        uint MapId)
    {
        public PlayerEntity AddPlayer(int charId, short x, short y)
        {
            var pc = new PlayerEntity(charId, charId, $"P{charId}", Guid.NewGuid(), MapId, x, y);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);
            return pc;
        }
        public void MarkWall(short x, short y)
        {
            // MapData stores cell flags; 1 typically = no-walk. The cell
            // array is private — use the raw byte array if accessible.
            // Without that, the test asserts only the sweep behavior.
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

    private sealed class NoSessions : ISessionManagerAccessor
    {
        public Map.Server.MapSessionData? GetByEntityId(EntityId entityId) => null;
    }
}
