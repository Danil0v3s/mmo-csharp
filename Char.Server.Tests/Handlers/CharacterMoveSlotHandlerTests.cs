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

public class CharacterMoveSlotHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMoveDisabled_ShouldFailReason1()
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

        var handler = new CharacterMoveSlotHandler(
            new InMemoryCharacterRepository(
            [
                new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, Moves = 2 }
            ]),
            new CharServerConfiguration
            {
                CharMove = new CharMoveConfiguration { Enabled = false }
            });

        await handler.HandleAsync(session, BuildMovePacket(0, 1, 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT, BitConverter.ToInt16(payload, 0));
        Assert.Equal((short)1, BitConverter.ToInt16(payload, 4));
        Assert.Equal((short)2, BitConverter.ToInt16(payload, 6));
    }

    [Fact]
    public async Task HandleAsync_WhenToUsedAndMoveToUsedDisabled_ShouldFailReason1()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, Moves = 2 },
            new CharEntity { CharId = 1002, AccountId = 2000000, CharNum = 1, DeleteDate = 0, Moves = 2 }
        ]);

        var handler = new CharacterMoveSlotHandler(
            repo,
            new CharServerConfiguration
            {
                CharMove = new CharMoveConfiguration { Enabled = true, MoveToUsed = false, Unlimited = false }
            });

        await handler.HandleAsync(session, BuildMovePacket(0, 1, 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)1, BitConverter.ToInt16(payload, 4));
    }

    [Fact]
    public async Task HandleAsync_WhenMoveToEmptySuccess_ShouldAck0AndDecrementMoves()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, Moves = 2 }
        ]);

        var handler = new CharacterMoveSlotHandler(
            repo,
            new CharServerConfiguration
            {
                CharMove = new CharMoveConfiguration { Enabled = true, MoveToUsed = false, Unlimited = false }
            });

        await handler.HandleAsync(session, BuildMovePacket(0, 2, 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveBufferedPackets(sockets.ClientSide);
        Assert.True(ContainsHeader(payload, PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT));
        Assert.True(ContainsAckSlotMove(payload, reason: 0, moves: 1));
        Assert.True(ContainsHeader(payload, PacketHeader.HC_ACCEPT_ENTER2));

        var moved = await repo.GetByIdAsync(1001);
        Assert.NotNull(moved);
        Assert.Equal((byte)2, moved!.CharNum);
        Assert.Equal((uint)1, moved.Moves);
    }

    [Fact]
    public async Task HandleAsync_WhenMoveToUsedEnabled_ShouldSwapSlotsAndAck0()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, CharNum = 0, DeleteDate = 0, Moves = 3 },
            new CharEntity { CharId = 1002, AccountId = 2000000, CharNum = 1, DeleteDate = 0, Moves = 5 }
        ]);

        var handler = new CharacterMoveSlotHandler(
            repo,
            new CharServerConfiguration
            {
                CharMove = new CharMoveConfiguration { Enabled = true, MoveToUsed = true, Unlimited = false }
            });

        await handler.HandleAsync(session, BuildMovePacket(0, 1, 0));
        await session.FlushPacketsAsync();

        var payload = ReceiveBufferedPackets(sockets.ClientSide);
        Assert.True(ContainsAckSlotMove(payload, reason: 0, moves: 2));

        var c1 = await repo.GetByIdAsync(1001);
        var c2 = await repo.GetByIdAsync(1002);
        Assert.NotNull(c1);
        Assert.NotNull(c2);
        Assert.Equal((byte)1, c1!.CharNum);
        Assert.Equal((byte)0, c2!.CharNum);
    }

    private static CH_MOVE_CHAR_SLOT BuildMovePacket(ushort from, ushort to, ushort remaining)
    {
        var packet = new CH_MOVE_CHAR_SLOT();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(from);
            writer.Write(to);
            writer.Write(remaining);
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

    private static bool ContainsAckSlotMove(byte[] payload, short reason, short moves)
    {
        var low = (byte)((short)PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT & 0xFF);
        var high = (byte)(((short)PacketHeader.HC_ACK_CHANGE_CHARACTER_SLOT >> 8) & 0xFF);
        for (var i = 0; i <= payload.Length - 8; i++)
        {
            if (payload[i] == low && payload[i + 1] == high)
            {
                var parsedReason = BitConverter.ToInt16(payload, i + 4);
                var parsedMoves = BitConverter.ToInt16(payload, i + 6);
                return parsedReason == reason && parsedMoves == moves;
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
                CharNum = source.CharNum,
                DeleteDate = source.DeleteDate,
                Moves = source.Moves,
                Name = source.Name
            };
        }
    }
}
