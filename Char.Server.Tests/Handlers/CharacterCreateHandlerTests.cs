using System.Net;
using System.Net.Sockets;
using Char.Server;
using Char.Server.Handlers;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Handlers;

public class CharacterCreateHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCharNewDisabled_ShouldSendRefuseMakeChar()
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
            CharacterSlots = 3
        };

        var handler = new CharacterCreateHandler(
            new InMemoryCharacterRepository([]),
            new CharServerConfiguration { CharNew = false });

        await handler.HandleAsync(session, BuildCreatePacket("Danilo", slot: 0, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_MAKECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0xFF, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenNameAlreadyExists_ShouldSendRefuseMakeCharNameExists()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, Name = "Danilo", DeleteDate = 0 }
        ]);

        var handler = new CharacterCreateHandler(repository, new CharServerConfiguration { CharNew = true });
        await handler.HandleAsync(session, BuildCreatePacket("Danilo", slot: 1, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_MAKECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0x00, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenNameDiffersOnlyByCaseAndIgnoringCaseDisabled_ShouldRefuseNameExists()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, Name = "danilo", DeleteDate = 0 }
        ]);

        var handler = new CharacterCreateHandler(
            repository,
            new CharServerConfiguration
            {
                CharNew = true,
                Char = new CharConfiguration { NameIgnoringCase = false }
            });
        await handler.HandleAsync(session, BuildCreatePacket("Danilo", slot: 1, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_MAKECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0x00, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenNameDiffersOnlyByCaseAndIgnoringCaseEnabled_ShouldAllowCreate()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, Name = "danilo", DeleteDate = 0 }
        ]);

        var handler = new CharacterCreateHandler(
            repository,
            new CharServerConfiguration
            {
                CharNew = true,
                Char = new CharConfiguration { NameIgnoringCase = true }
            });
        await handler.HandleAsync(session, BuildCreatePacket("Danilo", slot: 1, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACCEPT_MAKECHAR, BitConverter.ToInt16(payload, 0));
    }

    [Fact]
    public async Task HandleAsync_WhenValidRequest_ShouldSendAcceptMakeChar()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository([]);
        var handler = new CharacterCreateHandler(repository, new CharServerConfiguration { CharNew = true });
        await handler.HandleAsync(session, BuildCreatePacket("Danilo", slot: 0, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACCEPT_MAKECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal(2 + CharacterInfo.SerializedSize, payload.Length);

        var created = await repository.GetByNameAsync("Danilo");
        Assert.NotNull(created);
        Assert.Equal(2000000, created!.AccountId);
        Assert.Equal((byte)0, created.CharNum);
    }

    [Fact]
    public async Task HandleAsync_WhenNameNeedsNormalization_ShouldCreateWithNormalizedName()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository([]);
        var handler = new CharacterCreateHandler(repository, new CharServerConfiguration { CharNew = true });
        await handler.HandleAsync(session, BuildCreatePacket("  New\t\tName  ", slot: 0, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACCEPT_MAKECHAR, BitConverter.ToInt16(payload, 0));

        var created = await repository.GetByNameAsync("New Name");
        Assert.NotNull(created);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidNameStructure_ShouldRefuseMakeCharDenied()
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
            CharacterSlots = 3
        };

        var repository = new InMemoryCharacterRepository([]);
        var handler = new CharacterCreateHandler(
            repository,
            new CharServerConfiguration
            {
                CharNew = true,
                WispServerName = "Server"
            });

        await handler.HandleAsync(session, BuildCreatePacket("#Bad", slot: 0, hairColor: 1, hairStyle: 2, startJob: 0, sex: 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_MAKECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0xFF, payload[2]);
    }

    private static CH_MAKE_NEW_CHAR BuildCreatePacket(
        string name,
        byte slot,
        ushort hairColor,
        ushort hairStyle,
        uint startJob,
        byte sex)
    {
        var packet = new CH_MAKE_NEW_CHAR();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.WriteFixedString(name, PacketConstants.NAME_LENGTH);
            writer.Write(slot);
            writer.Write(hairColor);
            writer.Write(hairStyle);
            writer.Write(startJob);
            writer.Write(sex);
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

    private sealed class InMemoryCharacterRepository(IEnumerable<CharEntity> seed) : ICharacterRepository
    {
        private readonly Dictionary<int, CharEntity> _store = seed.ToDictionary(c => c.CharId, Clone);
        private int _nextId = seed.Select(c => c.CharId).DefaultIfEmpty(1000).Max() + 1;

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
            var created = Clone(entity);
            if (created.CharId <= 0)
            {
                created.CharId = _nextId++;
            }

            _store[created.CharId] = Clone(created);
            return Task.FromResult(created);
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
                Class = source.Class,
                BaseLevel = source.BaseLevel,
                JobLevel = source.JobLevel,
                Hair = source.Hair,
                HairColor = source.HairColor,
                LastMap = source.LastMap,
                LastX = source.LastX,
                LastY = source.LastY,
                SaveMap = source.SaveMap,
                SaveX = source.SaveX,
                SaveY = source.SaveY,
                DeleteDate = source.DeleteDate,
                Online = source.Online,
                Sex = source.Sex
            };
        }
    }
}
