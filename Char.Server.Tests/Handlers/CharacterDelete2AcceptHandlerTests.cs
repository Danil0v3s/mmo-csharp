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

public class CharacterDelete2AcceptHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenBirthdateMismatch_ShouldAckResult5()
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
            Birthdate = "1990-01-01"
        };

        var handler = new CharacterDelete2AcceptHandler(
            new InMemoryCharacterRepository([]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "910101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_CHAR_DELETE2_ACCEPT_ACK, BitConverter.ToInt16(payload, 0));
        Assert.Equal((uint)5, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenCharacterMissing_ShouldAckResult3()
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
            Birthdate = "1990-01-01"
        };

        var handler = new CharacterDelete2AcceptHandler(
            new InMemoryCharacterRepository([]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "900101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)3, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenRestricted_ShouldAckResult2()
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
            Birthdate = "1990-01-01"
        };

        var handler = new CharacterDelete2AcceptHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity
                {
                    CharId = 1001,
                    AccountId = 2000000,
                    GuildId = 10,
                    DeleteDate = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1)
                }
            ]),
            new CharServerConfiguration
            {
                Char = new CharConfiguration { CharDeleteRestriction = 0x02 }
            });

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "900101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)2, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenNotQueuedOrDelayNotPassed_ShouldAckResult4()
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
            Birthdate = "1990-01-01"
        };

        var handler = new CharacterDelete2AcceptHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity
                {
                    CharId = 1001,
                    AccountId = 2000000,
                    DeleteDate = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 1000)
                }
            ]),
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "900101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)4, BitConverter.ToUInt32(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenQueuedAndElapsed_ShouldAckResult1AndDeleteCharacter()
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
            Birthdate = "1990-01-01"
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 1001,
                AccountId = 2000000,
                DeleteDate = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 1)
            }
        ]);
        var handler = new CharacterDelete2AcceptHandler(repository, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "900101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveBufferedPackets(sockets.ClientSide);
        Assert.True(ContainsHeader(payload, PacketHeader.HC_CHAR_DELETE2_ACCEPT_ACK));
        Assert.False(await repository.ExistsAsync(1001));
    }

    [Fact]
    public async Task HandleAsync_WhenDeleteDelayDisabled_ShouldAllowWithoutQueuedDeleteDate()
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
            Birthdate = "1990-01-01"
        };

        var repository = new InMemoryCharacterRepository(
        [
            new CharEntity
            {
                CharId = 1001,
                AccountId = 2000000,
                DeleteDate = 0
            }
        ]);
        var handler = new CharacterDelete2AcceptHandler(
            repository,
            new CharServerConfiguration
            {
                Char = new CharConfiguration { CharDeleteDelay = 0 }
            });

        await handler.HandleAsync(session, BuildAcceptPacket(1001, "900101"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_CHAR_DELETE2_ACCEPT_ACK, BitConverter.ToInt16(payload, 0));
        Assert.Equal((uint)1, BitConverter.ToUInt32(payload, 6));
        Assert.False(await repository.ExistsAsync(1001));
    }

    [Fact]
    public void BirthdateMatches_ShouldMatchRathenaYYMMDDSemantics()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("900101");
        Assert.True(CharacterDelete2AcceptHandler.BirthdateMatches("1990-01-01", bytes));
        Assert.False(CharacterDelete2AcceptHandler.BirthdateMatches("1990-01-02", bytes));
    }

    private static CH_REQ_CHAR_DELETE2_ACCEPT BuildAcceptPacket(uint charId, string yymmdd)
    {
        var packet = new CH_REQ_CHAR_DELETE2_ACCEPT();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(charId);
            var raw = System.Text.Encoding.ASCII.GetBytes(yymmdd.PadRight(6, '\0'));
            writer.Write(raw, 0, 6);
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

    private static byte[] ReceiveBufferedPackets(Socket clientSide)
    {
        clientSide.ReceiveTimeout = 100;
        var all = new List<byte>(1024);
        var buffer = new byte[512];

        while (true)
        {
            try
            {
                var read = clientSide.Receive(buffer, SocketFlags.None);
                if (read <= 0)
                {
                    break;
                }

                all.AddRange(buffer.AsSpan(0, read).ToArray());
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                break;
            }
        }

        return all.ToArray();
    }

    private static bool ContainsHeader(byte[] payload, PacketHeader header)
    {
        var low = (byte)((short)header & 0xFF);
        var high = (byte)(((short)header >> 8) & 0xFF);
        for (var i = 0; i < payload.Length - 1; i++)
        {
            if (payload[i] == low && payload[i + 1] == high)
            {
                return true;
            }
        }

        return false;
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
                BaseLevel = source.BaseLevel,
                PartyId = source.PartyId,
                GuildId = source.GuildId,
                DeleteDate = source.DeleteDate
            };
        }
    }
}
