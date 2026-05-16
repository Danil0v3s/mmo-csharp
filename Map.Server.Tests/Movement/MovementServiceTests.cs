using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Tests.Movement;

/// <summary>
/// Integration tests for <see cref="MovementService"/>. The walk loop is
/// driven by <see cref="Core.Timer.Scheduler"/>; tests configure a tiny
/// speed (1ms per cell) so the walk completes well inside the polling
/// timeout.
/// </summary>
public class MovementServiceTests
{
    [Fact]
    public async Task TryStartWalk_OnEmptyMap_AdvancesEntityToTarget()
    {
        var (service, registry, mapId) = NewService(20, 20);
        var player = NewPlayer(1001, mapId, 5, 5);
        registry.Add(player);

        Assert.True(service.TryStartWalk(player, 8, 5));

        await WaitForArrival(player, 8, 5);
        Assert.Equal((short)8, player.X);
        Assert.Equal((short)5, player.Y);
        Assert.Null(player.Walk);
    }

    [Fact]
    public async Task TryStartWalk_DiagonalPath_StepsByDiagonalDirections()
    {
        var (service, registry, mapId) = NewService(20, 20);
        var player = NewPlayer(1002, mapId, 0, 0);
        registry.Add(player);

        Assert.True(service.TryStartWalk(player, 4, 4));

        await WaitForArrival(player, 4, 4);
        Assert.Equal((short)4, player.X);
        Assert.Equal((short)4, player.Y);
        // The final direction set should be NorthEast (1, 1).
        Assert.Equal((byte)Direction.NorthEast, player.Dir);
    }

    [Fact]
    public void TryStartWalk_NoPath_ReturnsFalse()
    {
        // Target is non-walkable.
        var cells = new byte[10 * 10];
        cells[7 * 10 + 7] = 1;
        var map = new MapData("blocked", 10, 10, cells);
        var (service, registry, mapId) = NewService(map);

        var player = NewPlayer(1003, mapId, 0, 0);
        registry.Add(player);

        Assert.False(service.TryStartWalk(player, 7, 7));
        Assert.Null(player.Walk);
    }

    [Fact]
    public async Task TryStartWalk_InterruptsPreviousWalk()
    {
        var (service, registry, mapId) = NewService(50, 50);
        var player = NewPlayer(1004, mapId, 0, 0);
        registry.Add(player);

        // Start a long-ish walk (within MaxWalkPath=32).
        Assert.True(service.TryStartWalk(player, 25, 0));
        // Immediately retarget — should cancel the original walk and start fresh.
        Assert.True(service.TryStartWalk(player, 5, 0));

        await WaitForArrival(player, 5, 0);
        // Final position is the second target, not the first.
        Assert.Equal((short)5, player.X);
    }

    [Fact]
    public void CancelWalk_StopsAdvancement()
    {
        var (service, registry, mapId) = NewService(50, 50);
        var player = NewPlayer(1005, mapId, 0, 0);
        registry.Add(player);

        Assert.True(service.TryStartWalk(player, 25, 0));
        service.CancelWalk(player);
        Assert.Null(player.Walk);
    }

    [Fact]
    public async Task Walk_KeepsSpatialIndexInSync()
    {
        var (service, registry, mapId) = NewService(20, 20);
        var player = NewPlayer(1006, mapId, 0, 0);
        registry.Add(player);

        Assert.True(service.TryStartWalk(player, 5, 5));
        await WaitForArrival(player, 5, 5);

        // After arrival, the spatial index should have the entity at (5, 5),
        // not the origin (0, 0).
        var atOrigin = registry.ForEachInRange(mapId, 0, 0, 0, EntityType.Pc);
        var atTarget = registry.ForEachInRange(mapId, 5, 5, 0, EntityType.Pc);
        Assert.Empty(atOrigin);
        Assert.Single(atTarget);
    }

    // --- helpers ---

    private static (IMovementService, IEntityRegistry, uint mapId) NewService(short xs, short ys)
        => NewService(new MapData($"test_{xs}x{ys}", xs, ys, new byte[xs * ys]));

    private static (IMovementService, IEntityRegistry, uint mapId) NewService(MapData map)
    {
        var world = new StubWorldRegistry(map);
        var registry = new EntityRegistry(world);
        var loggerFactory = LoggerFactory.Create(_ => { });
        var service = new MovementService(registry, world, loggerFactory.CreateLogger<MovementService>());
        return (service, registry, (uint)map.Name.GetHashCode());
    }

    private static PlayerEntity NewPlayer(int charId, uint mapId, short x, short y)
    {
        var p = new PlayerEntity(charId, charId * 10, $"P{charId}", Guid.NewGuid(), mapId, x, y);
        // Tiny speed so the walk loop finishes inside the test polling window.
        p.Speed = 1;
        return p;
    }

    private static async Task WaitForArrival(Entity entity, short targetX, short targetY)
    {
        // The scheduler fires callbacks on the thread pool; poll until the
        // entity reaches the target or we time out. 2 seconds is plenty for
        // the longest test path (≈40 cells × ~2ms diagonal step).
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            if (entity.X == targetX && entity.Y == targetY && entity.Walk == null)
            {
                return;
            }
            await Task.Delay(5);
        }
        throw new TimeoutException(
            $"Entity {entity.Id} did not arrive at ({targetX},{targetY}); actually at ({entity.X},{entity.Y}), walking={entity.Walk != null}");
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
