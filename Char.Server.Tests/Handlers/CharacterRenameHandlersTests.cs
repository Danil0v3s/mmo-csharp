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

public class CharacterRenameHandlersTests
{
    [Fact]
    public async Task ValidateRename_WhenValid_ShouldAck1AndStorePendingName()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(
            repo,
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildValidatePacket(2000000, 1001, "NewName"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACK_IS_VALID_CHARNAME, BitConverter.ToInt16(payload, 0));
        Assert.Equal((ushort)1, BitConverter.ToUInt16(payload, 2));
        Assert.Equal("NewName", session.PendingCharacterRename);
    }

    [Fact]
    public async Task ValidateRename_WhenNameNeedsNormalization_ShouldAck1AndStoreNormalizedName()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildValidatePacket(2000000, 1001, "  New\t\tName \r\n"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((short)PacketHeader.HC_ACK_IS_VALID_CHARNAME, BitConverter.ToInt16(payload, 0));
        Assert.Equal((ushort)1, BitConverter.ToUInt16(payload, 2));
        Assert.Equal("New Name", session.PendingCharacterRename);
    }

    [Fact]
    public async Task ValidateRename_WhenInvalid_ShouldAck0()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "Taken", DeleteDate = 0 },
            new CharEntity { CharId = 1002, AccountId = 2000001, Name = "NewName", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(
            repo,
            new CharServerConfiguration());

        await handler.HandleAsync(session, BuildValidatePacket(2000000, 1001, "NewName"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((ushort)0, BitConverter.ToUInt16(payload, 2));
    }

    [Fact]
    public async Task ValidateRename_WhenNameDiffersOnlyByCaseAndIgnoringCaseEnabled_ShouldAck1()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 },
            new CharEntity { CharId = 1002, AccountId = 2000001, Name = "newname", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(
            repo,
            new CharServerConfiguration
            {
                Char = new CharConfiguration { NameIgnoringCase = true }
            });

        await handler.HandleAsync(session, BuildValidatePacket(2000000, 1001, "NewName"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((ushort)1, BitConverter.ToUInt16(payload, 2));
    }

    [Fact]
    public async Task ValidateRename_WhenAccountIdMismatch_ShouldDisconnect()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildValidatePacket(2000001, 1001, "NewName"));

        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task ValidateRename_WhenReservedWispName_ShouldAck0()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);

        var handler = new CharacterRenameValidateHandler(
            repo,
            new CharServerConfiguration { WispServerName = "Server" });

        await handler.HandleAsync(session, BuildValidatePacket(2000000, 1001, "Server"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((ushort)0, BitConverter.ToUInt16(payload, 2));
    }

    [Fact]
    public async Task ApplyRename_WhenCharacterMismatch_ShouldSendNoResponse()
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

        var repo = new InMemoryCharacterRepository([]);
        var handler = new CharacterRenameApplyHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildApplyPacket(1001, "NewName"));
        await session.FlushPacketsAsync();

        sockets.ClientSide.ReceiveTimeout = 100;
        var buffer = new byte[16];
        var ex = Assert.Throws<SocketException>(() => sockets.ClientSide.Receive(buffer, SocketFlags.None));
        Assert.Equal(SocketError.TimedOut, ex.SocketErrorCode);
        Assert.True(session.IsAlive);
    }

    [Fact]
    public async Task ApplyRename_WhenNameTaken_ShouldAck4()
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

        var repo = new InMemoryCharacterRepository(
        [
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 },
            new CharEntity { CharId = 1002, AccountId = 2000001, Name = "TakenName", DeleteDate = 0 }
        ]);
        var handler = new CharacterRenameApplyHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildApplyPacket(1001, "TakenName"));
        await session.FlushPacketsAsync();

        var payload = ReceiveSinglePacket(sockets.ClientSide);
        Assert.Equal((uint)4, BitConverter.ToUInt32(payload, 2));
    }

    [Fact]
    public async Task ApplyRename_WhenValid_ShouldAck0UpdateNameAndResendWindow()
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
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);
        var handler = new CharacterRenameApplyHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildApplyPacket(1001, "NewName"));
        await session.FlushPacketsAsync();

        var payload = ReceiveBufferedPackets(sockets.ClientSide);
        Assert.True(ContainsHeader(payload, PacketHeader.HC_ACK_CHANGE_CHARNAME));
        Assert.True(ContainsResultForAckChangeCharname(payload, 0));
        Assert.True(ContainsHeader(payload, PacketHeader.HC_ACCEPT_ENTER2));

        var updated = await repo.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal("NewName", updated!.Name);
    }

    [Fact]
    public async Task ApplyRename_WhenNameNeedsNormalization_ShouldUseNormalizedName()
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
            new CharEntity { CharId = 1001, AccountId = 2000000, Name = "OldName", DeleteDate = 0 }
        ]);
        var handler = new CharacterRenameApplyHandler(repo, new CharServerConfiguration());

        await handler.HandleAsync(session, BuildApplyPacket(1001, " New\t\tName "));
        await session.FlushPacketsAsync();

        var payload = ReceiveBufferedPackets(sockets.ClientSide);
        Assert.True(ContainsResultForAckChangeCharname(payload, 0));

        var updated = await repo.GetByIdAsync(1001);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
    }

    private static CH_REQ_IS_VALID_CHARNAME BuildValidatePacket(uint accountId, uint charId, string newName)
    {
        var packet = new CH_REQ_IS_VALID_CHARNAME();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(accountId);
            writer.Write(charId);
            writer.WriteFixedString(newName, PacketConstants.NAME_LENGTH);
        }

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        packet.Read(reader);
        return packet;
    }

    private static CH_REQ_CHANGE_CHARNAME BuildApplyPacket(uint charId, string newName)
    {
        var packet = new CH_REQ_CHANGE_CHARNAME();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(charId);
            writer.WriteFixedString(newName, PacketConstants.NAME_LENGTH);
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

    private static bool ContainsResultForAckChangeCharname(byte[] payload, uint expectedResult)
    {
        var low = (byte)((short)PacketHeader.HC_ACK_CHANGE_CHARNAME & 0xFF);
        var high = (byte)(((short)PacketHeader.HC_ACK_CHANGE_CHARNAME >> 8) & 0xFF);
        for (var i = 0; i <= payload.Length - 6; i++)
        {
            if (payload[i] == low && payload[i + 1] == high)
            {
                var result = BitConverter.ToUInt32(payload, i + 2);
                return result == expectedResult;
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
            => Task.FromResult(_store.Values.Any(c => string.Equals(c.Name, name, StringComparison.Ordinal)));

        private static CharEntity Clone(CharEntity source)
        {
            return new CharEntity
            {
                CharId = source.CharId,
                AccountId = source.AccountId,
                Name = source.Name,
                DeleteDate = source.DeleteDate
            };
        }
    }
}
