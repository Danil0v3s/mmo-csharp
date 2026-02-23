using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;

namespace Char.Server;

public class CharServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;
    private readonly CharServerConfiguration _charConfiguration;
    private readonly Dictionary<int, MapAuthTicket> _mapAuthTickets = new();
    private readonly object _mapAuthLock = new();
    private bool _registeredToLoginServer;
    private int _registeredServerId = -1;
    private DateTime _nextRegistrationAttemptUtc = DateTime.MinValue;
    private DateTime _nextUserCountSyncUtc = DateTime.MinValue;
    private DateTime _nextAddressSyncUtc = DateTime.MinValue;
    private DateTime _nextOnlineSyncUtc = DateTime.MinValue;

    public CharServerImpl(
        ServerConfiguration configuration,
        ILogger<CharServerImpl> logger,
        IServiceProvider serviceProvider,
        PacketSystem packetSystem,
        SessionManager sessionManager
        )
        : base("CharServer", configuration, logger, packetSystem, sessionManager)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
        _charConfiguration = configuration as CharServerConfiguration
            ?? throw new InvalidOperationException("CharServerImpl requires CharServerConfiguration");
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
        await SetAllOfflineOnLoginServerAsync(cancellationToken);

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
            await _handlerRegistry.ProcessSessionPacketsAsync(session, Logger);
        }
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

    public bool IssueMapAuthTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint clientType,
        int ttlSeconds)
    {
        if (accountId <= 0 || characterId <= 0)
        {
            return false;
        }

        var expiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds <= 0 ? 60 : ttlSeconds);
        var ticket = new MapAuthTicket(
            AccountId: accountId,
            CharacterId: characterId,
            LoginId1: loginId1,
            LoginId2: loginId2,
            Sex: sex,
            ClientType: clientType,
            ExpiresAtUtc: expiresAt);

        lock (_mapAuthLock)
        {
            _mapAuthTickets[accountId] = ticket;
        }

        return true;
    }

    public bool TryConsumeMapAuthTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        out uint sex,
        out uint clientType)
    {
        lock (_mapAuthLock)
        {
            if (!_mapAuthTickets.TryGetValue(accountId, out var ticket))
            {
                sex = 0;
                clientType = 0;
                return false;
            }

            if (ticket.ExpiresAtUtc < DateTime.UtcNow ||
                ticket.CharacterId != characterId ||
                ticket.LoginId1 != loginId1 ||
                ticket.LoginId2 != loginId2)
            {
                _mapAuthTickets.Remove(accountId);
                sex = 0;
                clientType = 0;
                return false;
            }

            _mapAuthTickets.Remove(accountId);
            sex = ticket.Sex;
            clientType = ticket.ClientType;
            return true;
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
            await NotifyAccountStatusAsync(accountId, online: false);
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

    public async Task<CharacterServerAuthResponse?> AuthenticateAccountWithLoginServerAsync(
        int accountId,
        int loginId1,
        int loginId2,
        uint sex,
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.AuthenticateAccountForCharServerAsync(new CharacterServerAuthRequest
        {
            AccountId = accountId,
            LoginId1 = loginId1,
            LoginId2 = loginId2,
            Sex = sex,
            RequestId = requestId,
            CharServerId = (uint)Math.Max(_registeredServerId, 0)
        }, cancellationToken: cancellationToken);
    }

    public async Task NotifyAccountStatusAsync(
        int accountId,
        bool online,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        await client.NotifyAccountStatusAsync(new AccountStatusUpdateRequest
        {
            AccountId = accountId,
            CharServerId = Math.Max(_registeredServerId, 0),
            Online = online
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountDataResponse?> RequestFullAccountDataAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.GetFullAccountDataAsync(new AccountDataRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountInfoResponse?> RequestDetailedAccountInfoAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.GetAccountInfoAsync(new AccountInfoRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountStateUpdateResponse?> UpdateAccountStateAsync(
        int accountId,
        uint state,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.UpdateAccountStateAsync(new AccountStateUpdateRequest
        {
            AccountId = accountId,
            State = state
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountBanResponse?> BanAccountAsync(
        int accountId,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.BanAccountAsync(new AccountBanRequest
        {
            AccountId = accountId,
            DurationSeconds = durationSeconds
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountUnbanResponse?> UnbanAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.UnbanAccountAsync(new AccountUnbanRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountEmailChangeResponse?> ChangeAccountEmailAsync(
        int accountId,
        string currentEmail,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.ChangeAccountEmailAsync(new AccountEmailChangeRequest
        {
            AccountId = accountId,
            CurrentEmail = currentEmail,
            NewEmail = newEmail
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountSexChangeResponse?> ChangeAccountSexAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.ChangeAccountSexAsync(new AccountSexChangeRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountPincodeUpdateResponse?> UpdateAccountPincodeAsync(
        int accountId,
        string pincode,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.UpdateAccountPincodeAsync(new AccountPincodeUpdateRequest
        {
            AccountId = accountId,
            Pincode = pincode
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountPincodeAuthFailResponse?> NotifyPincodeAuthFailAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.NotifyPincodeAuthFailAsync(new AccountPincodeAuthFailRequest
        {
            AccountId = accountId
        }, cancellationToken: cancellationToken);
    }

    public async Task<GlobalAccRegUpdateResponse?> UpdateGlobalAccountRegistersAsync(
        int accountId,
        IEnumerable<GlobalAccRegEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var request = new GlobalAccRegUpdateRequest
        {
            AccountId = accountId
        };
        request.Entries.AddRange(entries);

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.UpdateGlobalAccountRegistersAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GlobalAccRegFetchResponse?> GetGlobalAccountRegistersAsync(
        int accountId,
        long charId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.GetGlobalAccountRegistersAsync(new GlobalAccRegFetchRequest
        {
            AccountId = accountId,
            CharId = charId
        }, cancellationToken: cancellationToken);
    }

    public async Task<AccountVipDataResponse?> RequestVipDataAsync(
        int accountId,
        uint flags,
        int durationSeconds,
        int mapServerId,
        CancellationToken cancellationToken = default)
    {
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return null;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        return await client.RequestVipDataAsync(new AccountVipDataRequest
        {
            AccountId = accountId,
            Flags = flags,
            DurationSeconds = durationSeconds,
            MapServerId = mapServerId
        }, cancellationToken: cancellationToken);
    }

    public async Task TriggerCharacterServerAddressSyncAsync(CancellationToken cancellationToken)
    {
        _nextAddressSyncUtc = DateTime.MinValue;
        await SyncCharacterServerAddressAsync(cancellationToken);
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

    private async Task EnsureRegisteredOnLoginServerAsync(CancellationToken cancellationToken)
    {
        if (_registeredToLoginServer || DateTime.UtcNow < _nextRegistrationAttemptUtc)
        {
            return;
        }

        _nextRegistrationAttemptUtc = DateTime.UtcNow.AddSeconds(5);

        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        var response = await client.RegisterCharacterServerAsync(new CharacterServerRegistrationRequest
        {
            Username = _charConfiguration.UserId,
            Password = _charConfiguration.Password,
            ServerName = _charConfiguration.ServerName,
            ServerAddress = $"{_charConfiguration.CharIp}:{_charConfiguration.CharPort}",
            ServerType = (uint)_charConfiguration.CharMaintenance,
            NewServer = _charConfiguration.CharNew
        }, cancellationToken: cancellationToken);

        if (response.Success)
        {
            _registeredToLoginServer = true;
            _registeredServerId = response.ServerId;
            Logger.LogInformation("Registered with LoginServer as char server id {ServerId}", _registeredServerId);
        }
        else
        {
            Logger.LogWarning(
                "Failed to register char server on LoginServer: {Error} (code {Code})",
                response.ErrorMessage,
                response.ResultCode);
        }
    }

    private async Task SyncUserCountAsync(CancellationToken cancellationToken)
    {
        if (!_registeredToLoginServer || _registeredServerId < 0 || DateTime.UtcNow < _nextUserCountSyncUtc)
        {
            return;
        }

        _nextUserCountSyncUtc = DateTime.UtcNow.AddSeconds(10);
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        var userCount = SessionManager.GetAllSessions().Count();
        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        await client.UpdateCharacterServerUserCountAsync(new CharacterServerUserCountUpdateRequest
        {
            ServerId = _registeredServerId,
            UserCount = (uint)userCount
        }, cancellationToken: cancellationToken);
    }

    private async Task SyncCharacterServerAddressAsync(CancellationToken cancellationToken)
    {
        if (!_registeredToLoginServer || _registeredServerId < 0 || DateTime.UtcNow < _nextAddressSyncUtc)
        {
            return;
        }

        _nextAddressSyncUtc = DateTime.UtcNow.AddSeconds(60);
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        if (!TryConvertIpv4ToUInt(_charConfiguration.CharIp, out var ip))
        {
            Logger.LogWarning("Cannot sync char server address because CharIp is invalid: {CharIp}", _charConfiguration.CharIp);
            return;
        }

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        await client.UpdateCharacterServerAddressAsync(new CharacterServerAddressUpdateRequest
        {
            ServerId = _registeredServerId,
            Ip = ip
        }, cancellationToken: cancellationToken);
    }

    private async Task SetAllOfflineOnLoginServerAsync(CancellationToken cancellationToken)
    {
        if (!_registeredToLoginServer || _registeredServerId < 0)
        {
            return;
        }

        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        try
        {
            var client = new LoginService.LoginServiceClient(loginSession.Channel);
            await client.SetAllOfflineForCharacterServerAsync(new CharacterServerSetAllOfflineRequest
            {
                ServerId = _registeredServerId
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to set all offline on login server for char server {ServerId}", _registeredServerId);
        }
    }

    private async Task SyncOnlineAccountsAsync(CancellationToken cancellationToken)
    {
        if (!_registeredToLoginServer || _registeredServerId < 0 || DateTime.UtcNow < _nextOnlineSyncUtc)
        {
            return;
        }

        _nextOnlineSyncUtc = DateTime.UtcNow.AddSeconds(30);
        var loginSession = ServerConnections.GetSessionsByType(ServerType.Login).FirstOrDefault();
        if (loginSession?.IsConnected != true)
        {
            return;
        }

        var accountIds = SessionManager.GetAllSessions()
            .OfType<CharSessionData>()
            .Where(session => session.AccountId.HasValue)
            .Select(session => session.AccountId!.Value)
            .Distinct()
            .ToList();

        var client = new LoginService.LoginServiceClient(loginSession.Channel);
        var request = new CharacterServerOnlineSyncRequest
        {
            ServerId = _registeredServerId
        };
        request.AccountIds.AddRange(accountIds);
        await client.SyncOnlineAccountsAsync(request, cancellationToken: cancellationToken);
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

    private record MapAuthTicket(
        int AccountId,
        long CharacterId,
        int LoginId1,
        int LoginId2,
        uint Sex,
        uint ClientType,
        DateTime ExpiresAtUtc
    );
}
