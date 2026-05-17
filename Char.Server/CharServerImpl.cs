using System.Net;
using System.Net.Sockets;
using Char.Server.Services;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;

namespace Char.Server;

public class CharServerImpl : GameLoopServer, ICharServerRuntime, IServerReadiness
{
    /// <summary>
    /// Char is "ready" only once the game loop is ticking AND it has
    /// successfully registered with the login server. Map servers can't
    /// register their maps with char until char itself is registered, and
    /// the client login flow won't proceed past AC_ACCEPT_LOGIN if no
    /// char server is registered with login.
    /// </summary>
    public bool IsReady => State == ServerState.Running && _serverState.IsRegisteredToLoginServer;

    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;
    private readonly CharServerConfiguration _charConfiguration;
    private readonly CharServerState _serverState;
    private readonly ILoginServerIpcService _loginServerIpc;
    private DateTime _nextRegistrationAttemptUtc = DateTime.MinValue;
    private DateTime _nextUserCountSyncUtc = DateTime.MinValue;
    private DateTime _nextAddressSyncUtc = DateTime.MinValue;
    private DateTime _nextOnlineSyncUtc = DateTime.MinValue;

    public ServerState State => _serverState.State;
    public int RegisteredServerId => _serverState.RegisteredServerId;
    public bool IsRegisteredToLoginServer => _serverState.IsRegisteredToLoginServer;
    public uint PartyShareLevel => _serverState.PartyShareLevel;
    public void SetPartyShareLevel(uint shareLevel) => _serverState.SetPartyShareLevel(shareLevel);

    public CharServerImpl(
        ServerConfiguration configuration,
        ILogger<CharServerImpl> logger,
        IServiceProvider serviceProvider,
        PacketSystem packetSystem,
        SessionManager sessionManager,
        ServerConnectionService connectionService,
        CharServerState serverState,
        ILoginServerIpcService loginServerIpc
    )
        : base("CharServer", configuration, logger, packetSystem, sessionManager)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
        _charConfiguration = configuration as CharServerConfiguration
            ?? throw new InvalidOperationException("CharServerImpl requires CharServerConfiguration");
        _serverState = serverState;
        _loginServerIpc = loginServerIpc;

        // Wire up the connection service to use this server's connection manager
        connectionService.SetConnectionManager(ServerConnections);
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        _serverState.SetState(base.State);
    }

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await base.StopAsync(cancellationToken);
        _serverState.SetState(base.State);
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("CharServer TCP listener started on port {Port}", Configuration.Port);

        _ = Task.Run(async () => await AcceptClientsAsync(cancellationToken), cancellationToken);

        await Task.CompletedTask;
    }

    protected override async Task StopTcpListenerAsync(CancellationToken cancellationToken)
    {
        if (_serverState.IsRegisteredToLoginServer && _serverState.RegisteredServerId >= 0)
        {
            await _loginServerIpc.SetAllOfflineAsync(_serverState.RegisteredServerId, cancellationToken);
            await _loginServerIpc.UnregisterCharacterServerAsync(_serverState.RegisteredServerId, cancellationToken);
            _serverState.SetRegistered(false, -1);
        }

        if (_listenerSocket != null)
        {
            _listenerSocket.Close();
            _listenerSocket.Dispose();
            _listenerSocket = null;
        }

        Logger.LogInformation("CharServer TCP listener stopped");
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listenerSocket != null)
        {
            try
            {
                var clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);
                var session = SessionManager.CreateSession<CharSessionData>(clientSocket);
                Logger.LogInformation("Client connected: {SessionId}", session.SessionId);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error accepting client");
            }
        }
    }

    protected override async Task ProcessIncomingPacketsAsync(double deltaTime, CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            while (session.IncomingPackets.TryDequeue(out var packet))
            {
                try
                {
                    if (session is CharSessionData charSession &&
                        ShouldRejectForPincodeGate(charSession, packet.Header, _charConfiguration))
                    {
                        Logger.LogWarning(
                            "Disconnecting session {SessionId}: packet 0x{Header:X4} is not allowed before pincode verification",
                            session.SessionId,
                            (short)packet.Header);
                        charSession.Disconnect(DisconnectReason.Kicked);
                        break;
                    }

                    bool handled = await _handlerRegistry.TryHandlePacketAsync(session, packet);
                    if (!handled)
                    {
                        Logger.LogError(
                            "No handler registered for packet {PacketType} (Header: 0x{Header:X4}) from session {SessionId}. Disconnecting client.",
                            packet.GetType().Name,
                            (short)packet.Header,
                            session.SessionId);
                        session.Disconnect(DisconnectReason.UnhandledPacket);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        ex,
                        "Error handling packet {PacketType} from session {SessionId}. Disconnecting client.",
                        packet.GetType().Name,
                        session.SessionId);
                    session.Disconnect(DisconnectReason.PacketHandlerError);
                    break;
                }
            }
        }
    }

    internal static bool ShouldRejectForPincodeGate(
        CharSessionData session,
        PacketHeader header,
        CharServerConfiguration configuration)
    {
        return ShouldRejectForPincodeGate(
            configuration.Pincode.Enabled,
            session.PincodeVerified,
            session.Pincode,
            header);
    }

    internal static bool ShouldRejectForPincodeGate(
        bool pincodeEnabled,
        bool pincodeVerified,
        string pincode,
        PacketHeader header)
    {
        if (!pincodeEnabled)
        {
            return false;
        }

        if (pincodeVerified)
        {
            return false;
        }

        // rAthena parser only hard-gates "other packets" when a pincode already exists.
        if (string.IsNullOrEmpty(pincode))
        {
            return false;
        }

        return !IsAllowedBeforePincodeVerification(header);
    }

    internal static bool IsAllowedBeforePincodeVerification(PacketHeader header)
    {
        return header == PacketHeader.CH_REQ_TO_CONNECT ||
               header == PacketHeader.CH_KEEP_ALIVE ||
               header == PacketHeader.CH_PINCODE_CHECK ||
               header == PacketHeader.CH_PINCODE_CHANGE ||
               header == PacketHeader.CH_REQ_PINCODE_WINDOW ||
               header == PacketHeader.CH_REQ_CHARLIST;
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        await EnsureRegisteredOnLoginServerAsync(cancellationToken);
        await SyncUserCountAsync(cancellationToken);
        await SyncCharacterServerAddressAsync(cancellationToken);
        await SyncOnlineAccountsAsync(cancellationToken);

        await Task.CompletedTask;
    }

    protected override async Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await session.FlushPacketsAsync();
        }
    }

    public async Task<int> ForceDisconnectAccountAsync(int accountId)
    {
        if (accountId <= 0)
        {
            return 0;
        }

        var disconnected = 0;
        foreach (var session in SessionManager.GetAllSessions())
        {
            if (session is CharSessionData charSession &&
                charSession.AccountId.HasValue &&
                charSession.AccountId.Value == accountId &&
                charSession.IsAlive)
            {
                charSession.Disconnect(DisconnectReason.Kicked);
                disconnected++;
            }
        }

        if (disconnected > 0)
        {
            Logger.LogInformation("Force-disconnected {Count} session(s) for account {AccountId}", disconnected, accountId);
            await _loginServerIpc.NotifyAccountStatusAsync(accountId, _serverState.RegisteredServerId, online: false);
        }

        return disconnected;
    }

    public async Task HandleAccountStatusBroadcastAsync(int accountId, bool isBan, uint value)
    {
        if (accountId <= 0)
        {
            return;
        }

        // Keep behavior simple and safe: if account gets blocked/banned, force disconnect.
        if ((!isBan && value != 0) || (isBan && value > (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            await ForceDisconnectAccountAsync(accountId);
        }
    }

    public async Task HandleAccountSexBroadcastAsync(int accountId, uint sex)
    {
        if (accountId <= 0)
        {
            return;
        }

        // Match legacy behavior where sex changes require online clients to reconnect.
        await ForceDisconnectAccountAsync(accountId);
        Logger.LogInformation("Received account sex update for account {AccountId} with sex code {Sex}", accountId, sex);
    }

    public async Task HandleVipDataPushAsync(
        int accountId,
        long vipTime,
        uint flags,
        uint groupId,
        int mapServerId,
        bool isVip,
        uint charSlots,
        uint charVip,
        uint oldGroup)
    {
        if (accountId <= 0)
        {
            return;
        }

        Logger.LogInformation(
            "Received VIP data push for account {AccountId}: isVip={IsVip}, vipTime={VipTime}, flags={Flags}, groupId={GroupId}, mapServer={MapServerId}, slots={CharSlots}, charVip={CharVip}, oldGroup={OldGroup}",
            accountId,
            isVip,
            vipTime,
            flags,
            groupId,
            mapServerId,
            charSlots,
            charVip,
            oldGroup);

        // Force refresh behavior for online sessions affected by VIP changes.
        await ForceDisconnectAccountAsync(accountId);
    }

    public void TriggerAddressSync()
    {
        _nextAddressSyncUtc = DateTime.MinValue;
    }

    private async Task EnsureRegisteredOnLoginServerAsync(CancellationToken cancellationToken)
    {
        if (_serverState.IsRegisteredToLoginServer || DateTime.UtcNow < _nextRegistrationAttemptUtc)
        {
            return;
        }

        _nextRegistrationAttemptUtc = DateTime.UtcNow.AddSeconds(5);

        var response = await _loginServerIpc.RegisterCharacterServerAsync(
            _charConfiguration.UserId,
            _charConfiguration.Password,
            _charConfiguration.ServerName,
            $"{_charConfiguration.CharIp}:{_charConfiguration.Port}",
            (ushort)_charConfiguration.Port,
            (uint)_charConfiguration.CharMaintenance,
            // CharNewDisplay drives the cosmetic "new server" badge in the
            // client server-list (AC_ACCEPT_LOGIN.CharServers[].New). It's a
            // separate concern from CharNew (which gates char *creation* in
            // CharacterCreateHandler); the two were previously conflated.
            _charConfiguration.CharNewDisplay != 0,
            cancellationToken);

        if (response?.Success == true)
        {
            _serverState.SetRegistered(true, response.ServerId);
            Logger.LogInformation("Registered with LoginServer as char server id {ServerId}", response.ServerId);
        }
        else
        {
            Logger.LogWarning(
                "Failed to register char server on LoginServer: {Error} (code {Code})",
                response?.ErrorMessage ?? "No response",
                response?.ResultCode ?? -1);
        }
    }

    private async Task SyncUserCountAsync(CancellationToken cancellationToken)
    {
        if (!_serverState.IsRegisteredToLoginServer || _serverState.RegisteredServerId < 0 || DateTime.UtcNow < _nextUserCountSyncUtc)
        {
            return;
        }

        _nextUserCountSyncUtc = DateTime.UtcNow.AddSeconds(10);
        var userCount = SessionManager.GetAllSessions().Count();
        await _loginServerIpc.UpdateUserCountAsync(_serverState.RegisteredServerId, (uint)userCount, cancellationToken);
    }

    private async Task SyncCharacterServerAddressAsync(CancellationToken cancellationToken)
    {
        if (!_serverState.IsRegisteredToLoginServer || _serverState.RegisteredServerId < 0 || DateTime.UtcNow < _nextAddressSyncUtc)
        {
            return;
        }

        _nextAddressSyncUtc = DateTime.UtcNow.AddSeconds(60);

        if (!TryConvertIpv4ToUInt(_charConfiguration.CharIp, out var ip))
        {
            Logger.LogWarning("Cannot sync char server address because CharIp is invalid: {CharIp}", _charConfiguration.CharIp);
            return;
        }

        await _loginServerIpc.UpdateServerAddressAsync(_serverState.RegisteredServerId, ip, cancellationToken);
    }

    private async Task SyncOnlineAccountsAsync(CancellationToken cancellationToken)
    {
        if (!_serverState.IsRegisteredToLoginServer || _serverState.RegisteredServerId < 0 || DateTime.UtcNow < _nextOnlineSyncUtc)
        {
            return;
        }

        _nextOnlineSyncUtc = DateTime.UtcNow.AddSeconds(30);

        var accountIds = SessionManager.GetAllSessions()
            .OfType<CharSessionData>()
            .Where(session => session.AccountId.HasValue)
            .Select(session => session.AccountId!.Value)
            .Distinct()
            .ToList();

        await _loginServerIpc.SyncOnlineAccountsAsync(_serverState.RegisteredServerId, accountIds, cancellationToken);
    }

    private static bool TryConvertIpv4ToUInt(string ipAddress, out uint ip)
    {
        ip = 0;
        if (!IPAddress.TryParse(ipAddress, out var parsed) || parsed.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = parsed.GetAddressBytes();
        ip = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        return true;
    }
}
