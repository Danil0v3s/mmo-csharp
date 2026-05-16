using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Tests.Entities;

public class EntityRegistryTests
{
    [Fact]
    public void AddGetRemove_RoundTrip()
    {
        var registry = NewRegistry();
        var p = NewPlayer(charId: 1001, x: 10, y: 10);

        registry.Add(p);
        Assert.True(registry.Contains(p.Id));
        Assert.Same(p, registry.Get(p.Id));

        var removed = registry.Remove(p.Id);
        Assert.Same(p, removed);
        Assert.False(registry.Contains(p.Id));
        Assert.Null(registry.Get(p.Id));
    }

    [Fact]
    public void Add_DuplicateId_Throws()
    {
        var registry = NewRegistry();
        registry.Add(NewPlayer(charId: 1, x: 1, y: 1));
        Assert.Throws<InvalidOperationException>(() =>
            registry.Add(NewPlayer(charId: 1, x: 2, y: 2)));
    }

    [Fact]
    public void Move_UpdatesEntityPosition_AndSpatialIndex()
    {
        var (registry, mapId) = NewRegistryWithMap("test", 50, 50);
        var p = NewPlayer(charId: 200, x: 5, y: 5, mapId: mapId);
        registry.Add(p);

        Assert.Single(registry.ForEachInRange(mapId, 5, 5, 0, EntityType.Pc));

        registry.Move(p.Id, 20, 20);

        Assert.Equal(20, p.X);
        Assert.Equal(20, p.Y);
        Assert.Empty(registry.ForEachInRange(mapId, 5, 5, 0, EntityType.Pc));
        Assert.Single(registry.ForEachInRange(mapId, 20, 20, 0, EntityType.Pc));
    }

    [Fact]
    public void ForEachInRange_FiltersByTypeMask()
    {
        var (registry, mapId) = NewRegistryWithMap("filter", 50, 50);

        var pc = NewPlayer(charId: 1, x: 10, y: 10, mapId: mapId);
        var npc = new NpcEntity(new EntityId(800_000_001), "Bob", 100, mapId, 11, 10);
        var mob = new MobEntity(new EntityId(400_000_001), 1002, "Poring", mapId, 12, 10);
        registry.Add(pc);
        registry.Add(npc);
        registry.Add(mob);

        var pcOnly = registry.ForEachInRange(mapId, 10, 10, 5, EntityType.Pc);
        Assert.Single(pcOnly);
        Assert.IsType<PlayerEntity>(pcOnly[0]);

        var pcOrMob = registry.ForEachInRange(mapId, 10, 10, 5, EntityType.Pc | EntityType.Mob);
        Assert.Equal(2, pcOrMob.Count);

        var all = registry.ForEachInRange(mapId, 10, 10, 5, EntityType.All);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void ForEachInRange_OnUnknownMap_ReturnsEmpty()
    {
        var registry = NewRegistry();
        Assert.Empty(registry.ForEachInRange(mapId: 999_999, 0, 0, 10, EntityType.All));
    }

    [Fact]
    public void All_EnumeratesEveryAddedEntity()
    {
        var registry = NewRegistry();
        registry.Add(NewPlayer(charId: 1, x: 1, y: 1));
        registry.Add(NewPlayer(charId: 2, x: 2, y: 2));
        registry.Add(NewPlayer(charId: 3, x: 3, y: 3));

        Assert.Equal(3, registry.All().Count());
        Assert.Equal(3, registry.Count);
    }

    // --- helpers ---

    private static EntityRegistry NewRegistry()
    {
        // A registry with no known maps — spatial queries return empty but
        // entity-id lookups still work.
        return new EntityRegistry(new StubWorldRegistry());
    }

    private static (EntityRegistry, uint mapId) NewRegistryWithMap(string name, short xs, short ys)
    {
        var map = new MapData(name, xs, ys, new byte[xs * ys]);
        var world = new StubWorldRegistry(map);
        var registry = new EntityRegistry(world);
        return (registry, (uint)name.GetHashCode());
    }

    private static PlayerEntity NewPlayer(int charId, short x, short y, uint mapId = 0)
        => new(charId, accountId: charId * 10, name: $"P{charId}", sessionId: Guid.NewGuid(), mapId, x, y);

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
