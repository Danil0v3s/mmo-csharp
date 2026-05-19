using System.Collections.Concurrent;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Google.Protobuf;
using Map.Server.Entities;
using Map.Server.Handlers.Storage;
using Map.Server.Inventory;
using Map.Server.Services;
using Map.Server.Session;
using Map.Server.Status;
using Map.Server.Storage;
using Map.Server.Tests.Session;
using Map.Server.World;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Storage;

/// <summary>
/// End-to-end wire coverage for storage. Mirrors rAthena
/// <c>clif_parse_MoveToKafra / MoveFromKafra / CloseKafra</c> —
/// asserts on the packet-id stream queued back to the client and
/// the resulting inventory + storage state.
/// </summary>
public class StorageHandlersTests
{
    [Fact]
    public async Task PutFromInventory_AddsToStorageAndEmitsAddPlusCount()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Identified = true },
        };
        await ctx.Service.OpenAsync(s);
        ClearOutbound(s);

        // client_index = server_index + 2 ⇒ slot 0 ⇒ 2.
        await ctx.MoveToHandler.HandleAsync(s, MakeToStore(clientIndex: 2, amount: 4));

        AssertSent(s, (ushort)PacketHeader.ZC_ADD_ITEM_TO_STORE);
        AssertSent(s, (ushort)PacketHeader.ZC_NOTIFY_STOREITEM_COUNTINFO);
        Assert.Equal(6u, s.Inventory[0].Amount);
        Assert.Single(s.Storage!.Items);
        Assert.Equal(4u, s.Storage.Items[0].Amount);
    }

    [Fact]
    public async Task TakeFromStorage_RemovesAndEmitsDeletePlusCount()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        // Seed inventory + storage by going through Open then the body→store path.
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 10, Identified = true },
        };
        await ctx.Service.OpenAsync(s);
        ctx.Service.AddFromInventory(s, invIndex: 0, amount: 7);
        ClearOutbound(s);

        // client_index = server_index + 1 ⇒ slot 0 ⇒ 1.
        await ctx.MoveFromHandler.HandleAsync(s, MakeFromStore(clientIndex: 1, amount: 3));

        AssertSent(s, (ushort)PacketHeader.ZC_DELETE_ITEM_FROM_STORE);
        AssertSent(s, (ushort)PacketHeader.ZC_NOTIFY_STOREITEM_COUNTINFO);
        Assert.Equal(4u, s.Storage!.Items[0].Amount);
        // Inventory got 3 back (merged into the original stack).
        Assert.Equal(6u, s.Inventory[0].Amount);
    }

    [Fact]
    public async Task TakeFromStorage_NotOpen_NoPacketsEmitted()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        ClearOutbound(s);

        await ctx.MoveFromHandler.HandleAsync(s, MakeFromStore(clientIndex: 1, amount: 1));

        Assert.Empty(OutboundQueue(s));
    }

    [Fact]
    public async Task Close_FlushesIpcAndEmitsCloseStore()
    {
        var ctx = New();
        var pc = ctx.AddPc(charId: 100, accountId: 1000);
        var s = ctx.SessionOf(pc);
        s.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 5 },
        };
        await ctx.Service.OpenAsync(s);
        ctx.Service.AddFromInventory(s, invIndex: 0, amount: 2);
        ClearOutbound(s);

        await ctx.CloseHandler.HandleAsync(s, new CZ_CLOSE_STORE());

        AssertSent(s, (ushort)PacketHeader.ZC_CLOSE_STORE);
        Assert.False(s.Storage!.IsOpen);
        Assert.Equal(1, ctx.Ipc.SaveCalls);
    }

    // --- wire helpers ---

    private static ushort HeaderOf(byte[] packet)
        => (ushort)(packet[0] | (packet[1] << 8));

    private static IReadOnlyList<byte[]> OutboundQueue(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) return q.ToArray();
        return Array.Empty<byte[]>();
    }

    private static void ClearOutbound(MapSessionData session)
    {
        var queueField = typeof(Core.Server.Network.ClientSession).GetField(
            "_outgoingPackets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (queueField?.GetValue(session) is ConcurrentQueue<byte[]> q) q.Clear();
    }

    private static void AssertSent(MapSessionData session, ushort packetId)
    {
        var ids = OutboundQueue(session).Select(HeaderOf).ToArray();
        Assert.Contains(packetId, ids);
    }

    // --- packet construction (reflection sidesteps private setters) ---

    private static CZ_MOVE_ITEM_FROM_BODY_TO_STORE MakeToStore(ushort clientIndex, int amount)
    {
        var p = new CZ_MOVE_ITEM_FROM_BODY_TO_STORE();
        typeof(CZ_MOVE_ITEM_FROM_BODY_TO_STORE).GetProperty("ClientIndex")!.SetValue(p, clientIndex);
        typeof(CZ_MOVE_ITEM_FROM_BODY_TO_STORE).GetProperty("Amount")!.SetValue(p, amount);
        return p;
    }

    private static CZ_MOVE_ITEM_FROM_STORE_TO_BODY MakeFromStore(ushort clientIndex, int amount)
    {
        var p = new CZ_MOVE_ITEM_FROM_STORE_TO_BODY();
        typeof(CZ_MOVE_ITEM_FROM_STORE_TO_BODY).GetProperty("ClientIndex")!.SetValue(p, clientIndex);
        typeof(CZ_MOVE_ITEM_FROM_STORE_TO_BODY).GetProperty("Amount")!.SetValue(p, amount);
        return p;
    }

    // --- harness ---

    private static TestContext New()
    {
        const string mapName = "test_map";
        var map = new MapData(mapName, 200, 200, new byte[200 * 200]);
        var world = new StubWorldRegistry(map);
        var entities = new EntityRegistry(world);
        var sessions = new InMemorySessions();
        var ipc = new StubStorageIpc();
        var svc = new StorageService(ipc, NullLogger<StorageService>.Instance);
        return new TestContext(
            svc, ipc, entities, sessions,
            new MoveItemFromBodyToStoreHandler(entities, svc, NullLogger<MoveItemFromBodyToStoreHandler>.Instance),
            new MoveItemFromStoreToBodyHandler(entities, svc, NullLogger<MoveItemFromStoreToBodyHandler>.Instance),
            new CloseStoreHandler(entities, svc, NullLogger<CloseStoreHandler>.Instance),
            (uint)mapName.GetHashCode());
    }

    private sealed record TestContext(
        StorageService Service,
        StubStorageIpc Ipc,
        EntityRegistry Entities,
        InMemorySessions Sessions,
        MoveItemFromBodyToStoreHandler MoveToHandler,
        MoveItemFromStoreToBodyHandler MoveFromHandler,
        CloseStoreHandler CloseHandler,
        uint MapId)
    {
        public PlayerEntity AddPc(int charId, int accountId)
        {
            var pc = new PlayerEntity(charId, accountId, $"P{charId}", Guid.NewGuid(), MapId, 50, 50);
            pc.Hp = pc.MaxHp = 1000;
            Entities.Add(pc);

            var sockets = TestSocketFactory.CreateSocketPair();
            var session = new MapSessionData(
                sockets.ServerSide, 30000,
                new PacketSystem().Factory, new PacketSystem().Registry,
                NullLogger.Instance)
            {
                AccountId = accountId,
                CharacterId = charId,
                AuthState = MapAuthState.Spawned,
                EntityId = pc.Id,
            };
            Sessions.Register(pc.Id, accountId, session);
            return pc;
        }

        public MapSessionData SessionOf(PlayerEntity pc) => Sessions.GetByEntityId(pc.Id)!;
    }

    private sealed class StubStorageIpc : ICharServerIpcServiceStorage
    {
        public int SaveCalls;
        public byte[] LastSavedBlob = Array.Empty<byte>();

        public Task<AccountStorageLoadResponse?> AccountStorageLoadAsync(
            int accountId, long characterId, CancellationToken cancellationToken = default)
            => Task.FromResult<AccountStorageLoadResponse?>(new AccountStorageLoadResponse
            {
                Success = true,
                Data = ByteString.Empty,
            });

        public Task<AccountStorageSaveResponse?> AccountStorageSaveAsync(
            int accountId, long characterId, byte[] data, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            LastSavedBlob = data;
            return Task.FromResult<AccountStorageSaveResponse?>(new AccountStorageSaveResponse
            {
                Success = true,
            });
        }

        public Task<GuildStorageLoadResponse?> GuildStorageLoadAsync(
            int guildId, CancellationToken cancellationToken = default)
            => Task.FromResult<GuildStorageLoadResponse?>(null);

        public Task<GuildStorageSaveResponse?> GuildStorageSaveAsync(
            int guildId, byte[] data, CancellationToken cancellationToken = default)
            => Task.FromResult<GuildStorageSaveResponse?>(null);

        public Task<StorageItemboundRetrieveResponse?> StorageItemboundRetrieveAsync(
            int accountId, long characterId, CancellationToken cancellationToken = default)
            => Task.FromResult<StorageItemboundRetrieveResponse?>(null);
    }

    private sealed class InMemorySessions : ISessionManagerAccessor
    {
        private readonly Dictionary<EntityId, MapSessionData> _byEid = new();
        private readonly Dictionary<int, MapSessionData> _byAcc = new();
        public void Register(EntityId id, int accountId, MapSessionData s)
        {
            _byEid[id] = s;
            _byAcc[accountId] = s;
        }
        public MapSessionData? GetByEntityId(EntityId entityId) => _byEid.GetValueOrDefault(entityId);
        public MapSessionData? GetByAccountId(int accountId) => _byAcc.GetValueOrDefault(accountId);
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
