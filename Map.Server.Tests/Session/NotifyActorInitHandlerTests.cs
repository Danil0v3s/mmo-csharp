using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Core.Server.Packets.Out.ZC;
using Map.Server;
using Map.Server.Entities;
using Map.Server.Handlers;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Tests.Visibility;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Session;

public class NotifyActorInitHandlerTests
{
    [Fact]
    public async Task UnauthenticatedSession_IsIgnored()
    {
        var ctx = NewContext();
        var session = ctx.NewSession(authState: MapAuthState.Unauthenticated);

        var handler = new NotifyActorInitHandler(
            ctx.Registry, ctx.Visibility, new StatusBroadcaster(),
            new StatusCalcService(), new NoOpPcDeathService(),
            new NoOpInventoryService(), new NoOpItemCatalog(),
            new NoOpItemHookDispatcher(), new NoOpComboDispatcher(),
            new Map.Server.Tests.Fakes.NoOpIntifService(),
            NullLogger<NotifyActorInitHandler>.Instance);

        await handler.HandleAsync(session, new CZ_NOTIFY_ACTORINIT());

        Assert.Null(session.EntityId);
        Assert.Equal(MapAuthState.Unauthenticated, session.AuthState);
        Assert.Empty(ctx.Registry.All());
    }

    [Fact]
    public async Task AuthenticatedSession_SpawnsPlayerAndBroadcasts()
    {
        var ctx = NewContext();
        var session = ctx.NewSession(
            authState: MapAuthState.Authenticated,
            characterId: 1500001,
            accountId: 2000001,
            name: "Hero");

        // Pre-populate an existing PC in view so the spawn broadcasts an
        // entered STANDENTRY to it and the spawning player receives one back.
        var viewer = new PlayerEntity(99999, 90000, "Viewer", Guid.NewGuid(), ctx.MapId, 105, 100);
        ctx.Registry.Add(viewer);

        var handler = new NotifyActorInitHandler(
            ctx.Registry, ctx.Visibility, new StatusBroadcaster(),
            new StatusCalcService(), new NoOpPcDeathService(),
            new NoOpInventoryService(), new NoOpItemCatalog(),
            new NoOpItemHookDispatcher(), new NoOpComboDispatcher(),
            new Map.Server.Tests.Fakes.NoOpIntifService(),
            NullLogger<NotifyActorInitHandler>.Instance);

        await handler.HandleAsync(session, new CZ_NOTIFY_ACTORINIT());

        Assert.Equal(MapAuthState.Spawned, session.AuthState);
        Assert.NotNull(session.EntityId);
        var spawned = ctx.Registry.Get(session.EntityId!.Value);
        Assert.NotNull(spawned);
        Assert.IsType<PlayerEntity>(spawned);

        var standEntries = ctx.Dispatcher.Sent
            .Where(s => s.packet is ZC_NOTIFY_STANDENTRY)
            .ToList();
        // Two STANDENTRY: one to the viewer about the new player, one to the
        // new player about the existing viewer.
        Assert.Equal(2, standEntries.Count);
    }

    [Fact]
    public async Task ExistingEntityForSameCharId_IsReplaced()
    {
        var ctx = NewContext();
        // Stale entry (simulating crash-recovery).
        var stale = new PlayerEntity(1500001, 2000001, "Stale", Guid.Empty, ctx.MapId, 50, 50);
        ctx.Registry.Add(stale);

        var session = ctx.NewSession(
            authState: MapAuthState.Authenticated,
            characterId: 1500001,
            accountId: 2000001,
            name: "Hero");

        var handler = new NotifyActorInitHandler(
            ctx.Registry, ctx.Visibility, new StatusBroadcaster(),
            new StatusCalcService(), new NoOpPcDeathService(),
            new NoOpInventoryService(), new NoOpItemCatalog(),
            new NoOpItemHookDispatcher(), new NoOpComboDispatcher(),
            new Map.Server.Tests.Fakes.NoOpIntifService(),
            NullLogger<NotifyActorInitHandler>.Instance);

        await handler.HandleAsync(session, new CZ_NOTIFY_ACTORINIT());

        var entity = ctx.Registry.Get(new EntityId(1500001));
        Assert.NotNull(entity);
        Assert.Equal("Hero", ((PlayerEntity)entity!).Name);
        Assert.Equal(session.SessionId, ((PlayerEntity)entity!).SessionId);
    }

    private static TestContext NewContext()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var registry = new EntityRegistry(world);
        var dispatcher = new RecordingDispatcher();
        var visibility = new VisibilityService(registry, dispatcher);
        return new TestContext(registry, visibility, dispatcher, (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        EntityRegistry Registry,
        IVisibilityService Visibility,
        RecordingDispatcher Dispatcher,
        uint MapId)
    {
        public MapSessionData NewSession(
            MapAuthState authState,
            int characterId = 1500001,
            int accountId = 2000001,
            string name = "Hero",
            short spawnX = 100,
            short spawnY = 100)
        {
            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide,
                heartbeatTimeout: 30000,
                packetFactory: new PacketSystem().Factory,
                sizeRegistry: new PacketSystem().Registry,
                logger: NullLogger.Instance)
            {
                AuthState = authState,
                CharacterId = characterId,
                AccountId = accountId,
                LoginId1 = 987654321,
                CharacterName = name,
                MapId = MapId,
                SpawnX = spawnX,
                SpawnY = spawnY,
            };
            return session;
        }
    }

    private sealed class NoOpInventoryService : IInventoryService
    {
        public Task LoadAsync(MapSessionData session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void SendInventoryList(MapSessionData session) { }
        public bool GiveItem(MapSessionData session, uint nameId, int amount) => true;
    }

    private sealed class NoOpPcDeathService : Map.Server.Combat.IPcDeathService
    {
        public void OnPcDead(PlayerEntity pc, Entity? source) { }
        public void Respawn(PlayerEntity pc) { }
        public bool IsDead(PlayerEntity pc) => false;
        public void SetSavepoint(int characterId, string mapName, short x, short y) { }
        public bool WarpToSavepoint(PlayerEntity pc) => false;
    }

    private sealed class NoOpItemCatalog : Map.Server.Items.IItemCatalog
    {
        public int Count => 0;
        public Core.Database.Entities.ItemEntity? Get(uint id) => null;
        public Core.Database.Entities.ItemEntity? GetByAegisName(string n) => null;
        public IEnumerable<Core.Database.Entities.ItemEntity> All() => Array.Empty<Core.Database.Entities.ItemEntity>();
        public void Reload() { }
    }

    private sealed class NoOpItemHookDispatcher : IItemHookDispatcher
    {
        public bool TryInvokeOnUse(MapSessionData session, PlayerEntity player, InventoryItem item) => false;
        public bool TryInvokeOnEquip(InventoryItem item, EquipBonusBundle bundle, PlayerEntity player, IReadOnlyList<InventoryItem> equipped) => false;
        public void TryInvokeOnUnequip(InventoryItem item, EquipBonusBundle bundle, PlayerEntity player, IReadOnlyList<InventoryItem> equipped) { }
    }

    private sealed class NoOpComboDispatcher : IComboDispatcher
    {
        public void ApplyActiveCombos(IReadOnlyList<InventoryItem> equipped, EquipBonusBundle bundle, PlayerEntity player) { }
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
