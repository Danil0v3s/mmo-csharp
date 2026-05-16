using System.Net;
using System.Net.Sockets;
using Char.Server;
using Char.Server.Handlers;
using Char.Server.Services;
using Core.Server.IPC;
using Core.Server.Packets;
using Core.Server.Packets.In.CH;
using Microsoft.Extensions.Logging;

namespace Char.Server.Tests.Handlers;

public class PincodeHandlerParityTests
{
    [Fact]
    public async Task PincodeCheck_WhenPincodeDisabled_ShouldDisconnect()
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
            PincodeVerified = false
        };

        var handler = new PincodeCheckHandler(
            loggerFactory.CreateLogger<PincodeCheckHandler>(),
            new CharServerConfiguration
            {
                Pincode = new PincodeConfiguration { Enabled = false }
            },
            new StubLoginServerIpcService());

        await handler.HandleAsync(session, BuildPincodeCheckPacket(2000000, "1234"));
        await session.FlushPacketsAsync();

        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task PincodeCheck_WhenMalformedPinPayload_ShouldDisconnect()
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
            Pincode = "1234",
            PincodeVerified = false
        };

        var handler = new PincodeCheckHandler(
            loggerFactory.CreateLogger<PincodeCheckHandler>(),
            new CharServerConfiguration
            {
                Pincode = new PincodeConfiguration { Enabled = true, MaxTry = 3 }
            },
            new StubLoginServerIpcService());

        await handler.HandleAsync(session, BuildPincodeCheckPacket(2000000, "12a4"));
        await session.FlushPacketsAsync();

        Assert.False(session.IsAlive);
    }

    [Fact]
    public async Task PincodeWindow_WhenPincodeDisabled_ShouldNotAlterVerificationState()
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
            PincodeVerified = false
        };

        var handler = new PincodeWindowHandler(
            loggerFactory.CreateLogger<PincodeWindowHandler>(),
            new CharServerConfiguration
            {
                Pincode = new PincodeConfiguration { Enabled = false }
            });

        await handler.HandleAsync(session, BuildPincodeWindowPacket(2000000));

        Assert.False(session.PincodeVerified);
        Assert.True(session.IsAlive);
    }

    private static CH_PINCODE_CHECK BuildPincodeCheckPacket(uint accountId, string pin)
    {
        var packet = new CH_PINCODE_CHECK();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(accountId);
            writer.Write(System.Text.Encoding.ASCII.GetBytes(pin.PadRight(4, '\0')));
        }

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        packet.Read(reader);
        return packet;
    }

    private static CH_REQ_PINCODE_WINDOW BuildPincodeWindowPacket(uint accountId)
    {
        var packet = new CH_REQ_PINCODE_WINDOW();
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(accountId);
        }

        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        packet.Read(reader);
        return packet;
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

    private sealed class StubLoginServerIpcService : ILoginServerIpcService
    {
        public Task<CharacterServerAuthResponse?> AuthenticateAccountAsync(int accountId, int loginId1, int loginId2, uint sex, int requestId, int charServerId, CancellationToken cancellationToken = default) => Task.FromResult<CharacterServerAuthResponse?>(null);
        public Task NotifyAccountStatusAsync(int accountId, int charServerId, bool online, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AccountOnlineAnywhereResponse?> IsAccountOnlineAnywhereAsync(int accountId, int excludeCharServerId, CancellationToken cancellationToken = default) => Task.FromResult<AccountOnlineAnywhereResponse?>(null);
        public Task<AccountDataResponse?> RequestFullAccountDataAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult<AccountDataResponse?>(null);
        public Task<AccountInfoResponse?> RequestDetailedAccountInfoAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult<AccountInfoResponse?>(null);
        public Task<AccountStateUpdateResponse?> UpdateAccountStateAsync(int accountId, uint state, CancellationToken cancellationToken = default) => Task.FromResult<AccountStateUpdateResponse?>(null);
        public Task<AccountBanResponse?> BanAccountAsync(int accountId, int durationSeconds, CancellationToken cancellationToken = default) => Task.FromResult<AccountBanResponse?>(null);
        public Task<AccountUnbanResponse?> UnbanAccountAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult<AccountUnbanResponse?>(null);
        public Task<AccountEmailChangeResponse?> ChangeAccountEmailAsync(int accountId, string currentEmail, string newEmail, CancellationToken cancellationToken = default) => Task.FromResult<AccountEmailChangeResponse?>(null);
        public Task<AccountSexChangeResponse?> ChangeAccountSexAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult<AccountSexChangeResponse?>(null);
        public Task<AccountPincodeUpdateResponse?> UpdateAccountPincodeAsync(int accountId, string pincode, CancellationToken cancellationToken = default) => Task.FromResult<AccountPincodeUpdateResponse?>(null);
        public Task<AccountPincodeAuthFailResponse?> NotifyPincodeAuthFailAsync(int accountId, CancellationToken cancellationToken = default) => Task.FromResult<AccountPincodeAuthFailResponse?>(null);
        public Task<GlobalAccRegUpdateResponse?> UpdateGlobalAccountRegistersAsync(int accountId, IEnumerable<GlobalAccRegEntry> entries, CancellationToken cancellationToken = default) => Task.FromResult<GlobalAccRegUpdateResponse?>(null);
        public Task<GlobalAccRegFetchResponse?> GetGlobalAccountRegistersAsync(int accountId, long charId, CancellationToken cancellationToken = default) => Task.FromResult<GlobalAccRegFetchResponse?>(null);
        public Task<AccountVipDataResponse?> RequestVipDataAsync(int accountId, uint flags, int durationSeconds, int mapServerId, CancellationToken cancellationToken = default) => Task.FromResult<AccountVipDataResponse?>(null);
        public Task<CharacterServerRegistrationResponse?> RegisterCharacterServerAsync(string username, string password, string serverName, string serverAddress, ushort socketPort, uint serverType, bool newServer, CancellationToken cancellationToken = default) => Task.FromResult<CharacterServerRegistrationResponse?>(null);
        public Task UpdateUserCountAsync(int serverId, uint userCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateServerAddressAsync(int serverId, uint ip, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetAllOfflineAsync(int serverId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UnregisterCharacterServerAsync(int serverId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SyncOnlineAccountsAsync(int serverId, IEnumerable<int> accountIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
