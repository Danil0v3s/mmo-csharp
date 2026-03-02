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

public class CharacterDeleteHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDeleteKeyInvalid_ShouldRefuseWithCode0()
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
            Email = "user@example.com",
            Birthdate = "1990-01-01"
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, DeleteDate = 0 }
        ]);

        var handler = new CharacterDeleteHandler(
            repository,
            new CharServerConfiguration { Char = new CharConfiguration { CharDeleteOption = 1 } });

        await handler.HandleAsync(session, BuildDeletePacket(1001, "wrong@example.com"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_DELETECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0x00, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenCharacterMissing_ShouldRefuseWithCode1()
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
            Email = "user@example.com"
        };

        var handler = new CharacterDeleteHandler(
            new InMemoryCharacterRepository([]),
            new CharServerConfiguration { Char = new CharConfiguration { CharDeleteOption = 1 } });

        await handler.HandleAsync(session, BuildDeletePacket(9999, "user@example.com"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_DELETECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0x01, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenGuildOrPartyRestricted_ShouldRefuseWithCode2()
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
            Email = "user@example.com"
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, GuildId = 10, DeleteDate = 0 }
        ]);

        var handler = new CharacterDeleteHandler(
            repository,
            new CharServerConfiguration
            {
                Char = new CharConfiguration
                {
                    CharDeleteOption = 1,
                    CharDeleteRestriction = 0x02
                }
            });

        await handler.HandleAsync(session, BuildDeletePacket(1001, "user@example.com"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_REFUSE_DELETECHAR, BitConverter.ToInt16(payload, 0));
        Assert.Equal((byte)0x02, payload[2]);
    }

    [Fact]
    public async Task HandleAsync_WhenAllowed_ShouldAcceptDeleteAndSoftDeleteCharacter()
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
            Email = "user@example.com"
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, DeleteDate = 0, Online = 1 }
        ]);

        var handler = new CharacterDeleteHandler(
            repository,
            new CharServerConfiguration { Char = new CharConfiguration { CharDeleteOption = 1 } });

        await handler.HandleAsync(session, BuildDeletePacket(1001, "user@example.com"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACCEPT_DELETECHAR, BitConverter.ToInt16(payload, 0));

        var updated = await repository.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.True(updated!.DeleteDate > 0);
        Assert.Equal((short)0, updated.Online);
    }

    [Fact]
    public void DeleteKeyMatches_ShouldMirrorRathenaEmailAndBirthdateChecks()
    {
        using var sockets = CreateSocketPair();
        var packetSystem = new PacketSystem();
        var session = new CharSessionData(
            sockets.ServerSide,
            heartbeatTimeout: 30000,
            packetSystem.Factory,
            packetSystem.Registry,
            LoggerFactory.Create(_ => { }).CreateLogger("session"))
        {
            Email = "a@a.com",
            Birthdate = "1990-01-01"
        };

        Assert.True(CharacterDeleteHandler.DeleteKeyMatches(session, "", 0x01));
        Assert.True(CharacterDeleteHandler.DeleteKeyMatches(session, "90-01-01", 0x02));
        Assert.False(CharacterDeleteHandler.DeleteKeyMatches(session, "wrong", 0x03));
    }

    private static CH_DELETE_CHAR BuildDeletePacket(uint charId, string key)
    {
        var packet = new CH_DELETE_CHAR();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(charId);
            writer.WriteFixedString(key, 50);
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
                CharNum = source.CharNum,
                Name = source.Name,
                BaseLevel = source.BaseLevel,
                GuildId = source.GuildId,
                PartyId = source.PartyId,
                DeleteDate = source.DeleteDate,
                Online = source.Online
            };
        }
    }
}
