using System.Net;
using System.Net.Sockets;
using Char.Server;
using Char.Server.Handlers;
using Char.Server.Services;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Handlers;

public class CharacterSelectAccessibleMapHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenValid_ShouldSendMapDataAndIssueTicket()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000,
            LoginId1 = 111,
            LoginId2 = 222,
            Sex = 0,
            ClientType = 0,
            IsAuthenticated = true,
            AccountDataLoaded = true
        };

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, LastMap = "unknown", Online = 0 }
        ]);
        var mapAuth = new MapAuthTicketService();
        var handler = new CharacterSelectAccessibleMapHandler(
            repo,
            mapAuth,
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(["prontera"]));

        await handler.HandleAsync(session, BuildSelectAccessibleMapPacket(slot: 0, mapNumber: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_SEND_MAP_DATA, BitConverter.ToInt16(payload, 0));
        Assert.Equal((uint)1001, BitConverter.ToUInt32(payload, 2));

        var updated = await repo.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal("prontera", updated!.LastMap);
        Assert.Equal(-2, updated.Online);

        var consumed = mapAuth.TryConsumeTicket(2000000, 1001, 111, 222, out _, out _);
        Assert.True(consumed);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidMapNumber_ShouldRejectEnter()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000,
            IsAuthenticated = true,
            AccountDataLoaded = true
        };

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, Online = 0 }
        ]);

        var handler = new CharacterSelectAccessibleMapHandler(
            repo,
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(["prontera"]));

        await handler.HandleAsync(session, BuildSelectAccessibleMapPacket(slot: 0, mapNumber: 127));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_ENTER, BitConverter.ToInt16(payload, 0));
        var updated = await repo.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal(-2, updated!.Online);
    }

    [Fact]
    public async Task HandleAsync_WhenCurrentMapIsAvailable_ShouldRejectEnter()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000,
            IsAuthenticated = true,
            AccountDataLoaded = true
        };

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, LastMap = "prontera", Online = 0 }
        ]);

        var handler = new CharacterSelectAccessibleMapHandler(
            repo,
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: true),
            new FakeMapServerRegistryService(["prontera"]));

        await handler.HandleAsync(session, BuildSelectAccessibleMapPacket(slot: 0, mapNumber: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_ENTER, BitConverter.ToInt16(payload, 0));
        var updated = await repo.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal(-2, updated!.Online);
    }

    [Fact]
    public async Task HandleAsync_WhenNoMapConnection_ShouldSendAuthResultServerClosed()
    {
        using var sockets = CreateSocketPair();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            loggerFactory.CreateLogger("session"))
        {
            AccountId = 2000000,
            IsAuthenticated = true,
            AccountDataLoaded = true
        };

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, LastMap = "unknown", Online = 0 }
        ]);

        var handler = new CharacterSelectAccessibleMapHandler(
            repo,
            new MapAuthTicketService(),
            new FakeServerConnectionService(hasMapConnection: false),
            new FakeMapServerRegistryService(["prontera"]));

        await handler.HandleAsync(session, BuildSelectAccessibleMapPacket(slot: 0, mapNumber: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.SC_NOTIFY_BAN, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)1, payload[2]);
        Assert.True(session.IsAlive);
    }

    private static CH_SELECT_ACCESSIBLE_MAPNAME BuildSelectAccessibleMapPacket(sbyte slot, sbyte mapNumber)
    {
        var packet = new CH_SELECT_ACCESSIBLE_MAPNAME();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(slot);
            writer.Write(mapNumber);
        }

        ms.Position = 0;
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

    private sealed class FakeMapServerRegistryService(IEnumerable<string> maps) : IMapServerRegistryService
    {
        private readonly HashSet<string> _maps = new(maps, StringComparer.OrdinalIgnoreCase);
        public int RegisterMaps(int mapServerId, IEnumerable<string> mapsToRegister) => 0;
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
                LastX = source.LastX,
                LastY = source.LastY,
                DeleteDate = source.DeleteDate,
                Online = source.Online
            };
        }
    }
}
