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

public class CharacterDelete2RequestHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCharacterNotFound_ShouldAckResult3()
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
            AccountId = 2000000
        };

        var handler = new CharacterDelete2RequestHandler(
            new InMemoryCharacterRepository([]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildDelete2RequestPacket(1001));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_CHAR_DELETE2_ACK, BitConverter.ToInt16(payload, 0));
        Assert.Equal((uint)1001, BitConverter.ToUInt32(payload, 2));
        Assert.Equal((uint)3, BitConverter.ToUInt32(payload, 6));
        Assert.Equal((uint)0, BitConverter.ToUInt32(payload, 10));
    }

    [Fact]
    public async Task HandleAsync_WhenAlreadyQueued_ShouldAckResult0()
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
            AccountId = 2000000
        };

        var handler = new CharacterDelete2RequestHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity { CharId = 1001, AccountId = 2000000, DeleteDate = 123 }
            ]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildDelete2RequestPacket(1001));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)0, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenGuildRestricted_ShouldAckResult4()
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
            AccountId = 2000000
        };

        var handler = new CharacterDelete2RequestHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity { CharId = 1001, AccountId = 2000000, GuildId = 5, DeleteDate = 0 }
            ]),
            new CharServerConfiguration
            {
                Char = new CharConfiguration { CharDeleteRestriction = 0x02 }
            });

        await handler.HandleAsync(session, BuildDelete2RequestPacket(1001));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)4, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenPartyRestricted_ShouldAckResult5()
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
            AccountId = 2000000
        };

        var handler = new CharacterDelete2RequestHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity { CharId = 1001, AccountId = 2000000, PartyId = 10, DeleteDate = 0 }
            ]),
            new CharServerConfiguration
            {
                Char = new CharConfiguration { CharDeleteRestriction = 0x01 }
            });

        await handler.HandleAsync(session, BuildDelete2RequestPacket(1001));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)5, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenAllowed_ShouldAckResult1AndSetDeleteDate()
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
            AccountId = 2000000
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, DeleteDate = 0 }
        ]);
        var handler = new CharacterDelete2RequestHandler(
            repository,
            new CharServerConfiguration
            {
                Char = new CharConfiguration { CharDeleteDelay = 86400 }
            });

        var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await handler.HandleAsync(session, BuildDelete2RequestPacket(1001));
        await session.FlushPacketsAsync();
        var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        var result = BitConverter.ToUInt32(payload, 6);
        var deleteDate = BitConverter.ToUInt32(payload, 10);

        Assert.Equal((uint)1, result);
        Assert.True(deleteDate >= (uint)(before + 86400));
        Assert.True(deleteDate <= (uint)(after + 86400 + 1));

        var updated = await repository.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal(deleteDate, updated!.DeleteDate);
    }

    private static CH_REQ_CHAR_DELETE2 BuildDelete2RequestPacket(uint charId)
    {
        var packet = new CH_REQ_CHAR_DELETE2();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(charId);
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
                GuildId = source.GuildId,
                PartyId = source.PartyId,
                DeleteDate = source.DeleteDate
            };
        }
    }
}
