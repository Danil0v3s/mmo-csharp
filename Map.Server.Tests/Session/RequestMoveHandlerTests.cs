using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Handlers;
using Map.Server.Movement;
using Map.Server.Session;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Session;

public class RequestMoveHandlerTests
{
    [Fact]
    public async Task UnspawnedSession_IsIgnored()
    {
        var ctx = NewContext();
        var session = ctx.NewSession(MapAuthState.Authenticated, entityId: null);

        var handler = new RequestMoveHandler(
            ctx.Registry,
            ctx.Movement,
            ctx.Visibility,
            NullLogger<RequestMoveHandler>.Instance);

        await handler.HandleAsync(session, CreatePacket(105, 100));

        Assert.Empty(ctx.Dispatcher.Sent);
    }

    [Fact]
    public async Task SpawnedSession_StartsWalkAndBroadcasts()
    {
        var ctx = NewContext();
        var player = new PlayerEntity(1500001, 2000001, "Hero", Guid.NewGuid(), ctx.MapId, 100, 100);
        ctx.Registry.Add(player);
        var viewer = new PlayerEntity(99999, 90000, "Viewer", Guid.NewGuid(), ctx.MapId, 105, 100);
        ctx.Registry.Add(viewer);
        var session = ctx.NewSession(MapAuthState.Spawned, entityId: player.Id);
        // The handler reads session.EntityId so the SessionId on the session
        // must match the player's SessionId for visibility echo to work.
        // (Send-to-self goes to player.SessionId via NotifyMoveToArea which
        // excludes the source — viewer is the recipient.)
        await new RequestMoveHandler(
            ctx.Registry, ctx.Movement, ctx.Visibility,
            NullLogger<RequestMoveHandler>.Instance)
            .HandleAsync(session, CreatePacket(105, 100));

        // Echo to the walker is enqueued on the session directly (not via
        // dispatcher); just verify the broadcast went out via dispatcher.
        var moveBroadcasts = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_MOVE)
            .ToList();
        Assert.Single(moveBroadcasts);
        Assert.Equal(viewer.SessionId, moveBroadcasts[0].sessionId);
        Assert.NotNull(player.Walk);
    }

    private static CZ_REQUEST_MOVE CreatePacket(short targetX, short targetY)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        PositionPacker.WritePos(bw, targetX, targetY, 0);
        ms.Position = 0;
        return CZ_REQUEST_MOVE.Create(new BinaryReader(ms));
    }

    private static TestContext NewContext()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var registry = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(registry, dispatcher);
        var movement = new MovementService(registry, world, visibility, NullLogger<MovementService>.Instance);
        return new TestContext(registry, movement, visibility, dispatcher, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        EntityRegistry Registry,
        IMovementService Movement,
        IVisibilityService Visibility,
        RecordingDispatcher Dispatcher,
        uint MapId)
    {
        public MapSessionData NewSession(MapAuthState authState, EntityId? entityId)
        {
            var sockets = TestSocketFactory.CreateSocketPair();
            return new MapSessionData(
                sockets.ServerSide,
                heartbeatTimeout: 30000,
                packetFactory: new PacketSystem().Factory,
                sizeRegistry: new PacketSystem().Registry,
                logger: NullLogger.Instance)
            {
                AuthState = authState,
                EntityId = entityId,
            };
        }
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
