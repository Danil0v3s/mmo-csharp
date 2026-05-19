using Core.Server.Packets;
using Map.Server.Inventory;
using Map.Server.Session;
using Map.Server.Storage;
using Map.Server.Tests.Session;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Storage;

public class StorageServiceTests
{
    [Fact]
    public void AddFromInventory_NotOpen_ReturnsNotOpen()
    {
        var (svc, session) = New();
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 5 },
        };
        var result = svc.AddFromInventory(session, 0, 1);
        Assert.Equal(StorageOpResult.NotOpen, result);
    }

    [Fact]
    public void AddFromInventory_MovesAndDecrementsSource()
    {
        var (svc, session) = New(open: true);
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 5 },
        };

        Assert.Equal(StorageOpResult.Ok, svc.AddFromInventory(session, 0, 3));

        Assert.Single(session.Storage!.Items);
        Assert.Equal(3u, session.Storage.Items[0].Amount);
        Assert.Equal(2u, session.Inventory[0].Amount);
    }

    [Fact]
    public void AddFromInventory_FullStack_RemovesInvSlot()
    {
        var (svc, session) = New(open: true);
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 5, Id = 99 },
        };

        Assert.Equal(StorageOpResult.Ok, svc.AddFromInventory(session, 0, 5));
        Assert.Empty(session.Inventory);
        Assert.Contains(99, session.RemovedInventoryIds);
    }

    [Fact]
    public void AddFromInventory_MergesIntoExistingStack()
    {
        var (svc, session) = New(open: true);
        session.Storage!.Items.Add(new StorageItem { NameId = 501, Amount = 10 });
        session.Inventory = new List<InventoryItem>
        {
            new() { ServerIndex = 0, NameId = 501, Amount = 5 },
        };

        Assert.Equal(StorageOpResult.Ok, svc.AddFromInventory(session, 0, 5));
        Assert.Single(session.Storage.Items);
        Assert.Equal(15u, session.Storage.Items[0].Amount);
    }

    [Fact]
    public void TakeToInventory_MovesAndCompactsStorage()
    {
        var (svc, session) = New(open: true);
        session.Storage!.Items.Add(new StorageItem { NameId = 501, Amount = 10 });
        session.Storage.Items.Add(new StorageItem { NameId = 502, Amount = 3 });

        Assert.Equal(StorageOpResult.Ok, svc.TakeToInventory(session, 0, 10));

        // First slot fully removed; second slot compacts to index 0.
        Assert.Single(session.Storage.Items);
        Assert.Equal(0, session.Storage.Items[0].ServerIndex);
        Assert.NotNull(session.Inventory);
        Assert.Single(session.Inventory!);
        Assert.Equal(10u, session.Inventory[0].Amount);
    }

    [Fact]
    public void Codec_RoundTrips()
    {
        var items = new List<StorageItem>
        {
            new() { NameId = 501, Amount = 5, Refine = 3, Card0 = 4001 },
            new() { NameId = 502, Amount = 1 },
        };
        var bytes = StorageCodec.Encode(items);
        var back = StorageCodec.Decode(bytes);

        Assert.Equal(2, back.Count);
        Assert.Equal(501u, back[0].NameId);
        Assert.Equal(5u, back[0].Amount);
        Assert.Equal((byte)3, back[0].Refine);
        Assert.Equal(4001u, back[0].Card0);
    }

    private static (StorageService Service, MapSessionData Session) New(bool open = false)
    {
        var ipc = new StubIpc();
        var svc = new StorageService(ipc, NullLogger<StorageService>.Instance);
        var sockets = TestSocketFactory.CreateSocketPair();
        var session = new MapSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetFactory: new PacketSystem().Factory,
            sizeRegistry: new PacketSystem().Registry,
            logger: NullLogger.Instance)
        {
            AccountId = 1,
            CharacterId = 100,
            AuthState = MapAuthState.Spawned,
        };
        if (open)
        {
            session.Storage = new StorageState { IsOpen = true };
        }
        return (svc, session);
    }

    /// <summary>Minimal stub — all storage IPC calls return success.</summary>
    private sealed class StubIpc : Map.Server.Services.ICharServerIpcServiceStorage
    {
        public Task<Core.Server.IPC.AccountStorageLoadResponse?> AccountStorageLoadAsync(int accountId, long characterId, CancellationToken ct = default)
            => Task.FromResult<Core.Server.IPC.AccountStorageLoadResponse?>(new() { Success = true });
        public Task<Core.Server.IPC.AccountStorageSaveResponse?> AccountStorageSaveAsync(int accountId, long characterId, byte[] data, CancellationToken ct = default)
            => Task.FromResult<Core.Server.IPC.AccountStorageSaveResponse?>(new() { Success = true });
        public Task<Core.Server.IPC.GuildStorageLoadResponse?> GuildStorageLoadAsync(int guildId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Core.Server.IPC.GuildStorageSaveResponse?> GuildStorageSaveAsync(int guildId, byte[] data, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Core.Server.IPC.StorageItemboundRetrieveResponse?> StorageItemboundRetrieveAsync(int accountId, long characterId, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
