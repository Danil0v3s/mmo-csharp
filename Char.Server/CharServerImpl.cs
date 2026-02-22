using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;

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

    public CharServerImpl(
        ServerConfiguration configuration,
        ILogger<CharServerImpl> logger,
        IServiceProvider serviceProvider)
        : base("CharServer", configuration, logger)
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
                var session = SessionManager.CreateSession(clientSocket);
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
