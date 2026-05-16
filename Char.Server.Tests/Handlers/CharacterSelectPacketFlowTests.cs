using System.Net;
using System.Net.Sockets;
using Char.Server.Handlers;
using Char.Server.Services;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Handlers;

public class CharacterSelectPacketFlowTests
{
    [Fact]
    public async Task HandleAsync_ValidSelection_ShouldSendMapDataAndIssueMapAuthTicket()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        session.AccountId = 2000000;
        session.LoginId1 = 111;
        session.LoginId2 = 222;
        session.Sex = 0;
        session.ClientType = 0;
        session.IsAuthenticated = true;
        session.AccountDataLoaded = true;
        session.PincodeVerified = true;

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 1001,
                AccountId = 2000000,
                CharNum = 2,
                Name = "Danilo",
                LastMap = "prontera",
                DeleteDate = 0,
                Online = 0
            }
        ]);

        var mapAuth = new MapAuthTicketService();
        var handler = new CharacterSelectHandler(
            loggerFactory.CreateLogger<CharacterSelectHandler>(),
            repository,
            mapAuth,
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(hasMaps: ["prontera"]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildSelectPacket(slot: 2));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_SEND_MAP_DATA, BitConverter.ToInt16(payload, 0));
        Assert.Equal((uint)1001, BitConverter.ToUInt32(payload, 2));

        var stored = await repository.GetByIdAsync(1001);
        Assert.NotNull(stored);
        Assert.Equal(-2, stored!.Online);

        var consumed = mapAuth.TryConsumeTicket(
            accountId: 2000000,
            characterId: 1001,
            loginId1: 111,
            loginId2: 222,
            out _,
            out _);
        Assert.True(consumed);
    }

    [Fact]
    public async Task HandleAsync_NoMapConnection_ShouldSendNotifyBanResult()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        session.AccountId = 2000001;
        session.IsAuthenticated = true;
        session.AccountDataLoaded = true;
        session.PincodeVerified = true;

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 2001, AccountId = 2000001, CharNum = 0, DeleteDate = 0 }
        ]);

        var handler = new CharacterSelectHandler(
            loggerFactory.CreateLogger<CharacterSelectHandler>(),
            repository,
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: false),
            new FakeMapServerRegistryService(hasMaps: []),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildSelectPacket(slot: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_NOTIFY_ACCESSIBLE_MAPNAME, BitConverter.ToInt16(payload, 0));
        Assert.True(session.IsAlive);
    }

    [Fact]
    public async Task HandleAsync_WhenSelectedMapIsUnavailable_ShouldSendAccessibleMapsAndMarkOnline()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        session.AccountId = 2000002;
        session.IsAuthenticated = true;
        session.AccountDataLoaded = true;
        session.PincodeVerified = true;

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 2002, AccountId = 2000002, CharNum = 0, LastMap = "missing_map", DeleteDate = 0, Online = 0 }
        ]);

        var handler = new CharacterSelectHandler(
            loggerFactory.CreateLogger<CharacterSelectHandler>(),
            repository,
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(hasMaps: []),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildSelectPacket(slot: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_NOTIFY_ACCESSIBLE_MAPNAME, BitConverter.ToInt16(payload, 0));

        var updated = await repository.GetByIdAsync(2002);
        Assert.NotNull(updated);
        Assert.Equal(-2, updated!.Online);
    }

    [Fact]
    public async Task HandleAsync_WhenAccountDataNotLoaded_ShouldRejectAsOutOfOrder()
    {
        // Regression for "out-of-order char-select before charlist": when CH_SELECT_CHAR
        // arrives after auth but before the account-data continuation completes, the
        // handler must reject with HC_REFUSE_ENTER. Mirrors the charlist precondition
        // check (CharacterListFlowService.IsOutOfOrderCharlistRequest).
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        session.AccountId = 2000003;
        session.IsAuthenticated = true;
        session.AccountDataLoaded = false; // auth done, account data NOT yet loaded
        session.PincodeVerified = true;

        var handler = new CharacterSelectHandler(
            loggerFactory.CreateLogger<CharacterSelectHandler>(),
            new InMemoryCharacterRepository([]),
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(hasMaps: []),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildSelectPacket(slot: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_ENTER, BitConverter.ToInt16(payload, 0));
        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task HandleAsync_Unauthenticated_ShouldSendRefuseEnterAndDisconnect()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"));

        var handler = new CharacterSelectHandler(
            loggerFactory.CreateLogger<CharacterSelectHandler>(),
            new InMemoryCharacterRepository([]),
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(hasMaps: []),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildSelectPacket(slot: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_ENTER, BitConverter.ToInt16(payload, 0));
        Assert.False(session.IsAlive);
    }

    private static CH_SELECT_CHAR BuildSelectPacket(byte slot)
    {
        var packet = new CH_SELECT_CHAR();
        using var ms = new MemoryStream([slot]);
        using var reader = new BinaryReader(ms);
        packet.Read(reader);
        return packet;
    }

    private static byte[] ReceiveSinglePacket(Socket clientSide)
    {
        clientSide.ReceiveTimeout = 1000;
        var buffer = new byte[512];
        var read = clientSide.Receive(buffer, SocketFlags.None);
        return buffer[..read];
    }

    private static SocketPair CreateSocketPair()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;

        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(endpoint);

        var server = listener.AcceptSocket();
        listener.Stop();

        return new SocketPair(server, client);
    }

    private sealed record SocketPair(Socket ServerSide, Socket ClientSide) : IDisposable
    {
        public void Dispose()
        {
            try { ServerSide.Close(); } catch { }
            try { ClientSide.Close(); } catch { }
            ServerSide.Dispose();
            ClientSide.Dispose();
        }
    }

    private sealed class FakeServerConnectionService(bool hasMapConnection) : IServerConnectionService
    {
        public IEnumerable<ServerSession> GetSessionsByType(ServerType serverType) => [];
        public IEnumerable<ServerSession> GetAllConnectedSessions() => [];
        public ServerSession? GetSessionByName(string serverName) => null;
        public bool HasConnection(ServerType serverType) => serverType == ServerType.Map && hasMapConnection;
        public int GetConnectionCount(ServerType serverType) => HasConnection(serverType) ? 1 : 0;
    }

    private sealed class FakeMapServerRegistryService(IEnumerable<string> hasMaps) : IMapServerRegistryService
    {
        private readonly HashSet<string> _maps = new(hasMaps, StringComparer.OrdinalIgnoreCase);
        public int RegisterMaps(int mapServerId, IEnumerable<string> maps) => 0;
        public void SetUserCount(int mapServerId, int userCount) { }
        public bool TryGetUserCount(int mapServerId, out int userCount) { userCount = 0; return false; }
        public void SetAddress(int mapServerId, uint ip, uint port) { }
        public bool HasServer(int mapServerId) => mapServerId > 0;
        public bool ContainsMap(string mapName) => _maps.Contains(mapName);
        public bool TryGetMapAddress(string mapName, out uint ip, out ushort port)
        {
            if (_maps.Contains(mapName))
            {
                ip = 0x7F000001;
                port = 5121;
                return true;
            }

            ip = 0;
            port = 0;
            return false;
        }
    }

    private sealed class InMemoryCharacterRepository(IEnumerable<CharEntity> seed) : ICharacterRepository
    {
        private readonly Dictionary<int, CharEntity> _store = seed.ToDictionary(c => c.CharId, Clone);

        public Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(charId, out var entity) ? Clone(entity) : null);

        public Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_store.Values.FirstOrDefault(c => c.Name == name) is { } entity ? Clone(entity) : null);

        public Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Where(c => c.AccountId == accountId).Select(Clone).ToList());

        public Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Where(c => c.Online != 0).Select(Clone).ToList());

        public Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharEntity>>(_store.Values.Select(Clone).ToList());

        public Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default)
        {
            _store[entity.CharId] = Clone(entity);
            return Task.FromResult(Clone(entity));
        }

        public Task UpdateAsync(CharEntity entity, CancellationToken ct = default)
        {
            _store[entity.CharId] = Clone(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int charId, CancellationToken ct = default)
        {
            _store.Remove(charId);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int charId, CancellationToken ct = default)
            => Task.FromResult(_store.ContainsKey(charId));

        public Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_store.Values.Any(c => c.Name == name));

        private static CharEntity Clone(CharEntity source)
        {
            return new CharEntity
            {
                CharId = source.CharId,
                AccountId = source.AccountId,
                CharNum = source.CharNum,
                Name = source.Name,
                LastMap = source.LastMap,
                SaveMap = source.SaveMap,
                DeleteDate = source.DeleteDate,
                Online = source.Online
            };
        }
    }
}
