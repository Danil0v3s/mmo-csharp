using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Monitoring;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.AC;
using Login.Server.Repository.Api;
using Login.Server.Security;

namespace Login.Server;

public class LoginServerImpl : GameLoopServer, IServerReadiness
{
    /// <summary>
    /// Login has no peer-registration dependency: once the game loop is
    /// running (TCP listener bound, DB/IP-ban warmup done), it's ready.
    /// Char + Map will only flip their own readiness once they've talked
    /// to us, so this needs to be true first.
    /// </summary>
    public bool IsReady => State == ServerState.Running;

    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;
    private readonly ILoginSecurityService _loginSecurityService;
    private readonly ICharServerIpcService _charServerIpcService;
    private readonly ICharServerRegistry _charServerRegistry;
    private readonly ILoginDataRepository _loginDataRepository;
    private readonly EndpointLivenessMonitor _charServerLivenessMonitor;
    private readonly LoginServerState _serverState;
    private DateTime _nextIpBanCleanupUtc = DateTime.MinValue;
    private DateTime _nextCharIpSyncUtc = DateTime.MinValue;
    private DateTime _nextOrphanSweepUtc = DateTime.MinValue;

    public LoginServerImpl(
        ServerConfiguration configuration,
        ILogger<LoginServerImpl> logger,
        IServiceProvider serviceProvider,
        ILoginSecurityService loginSecurityService,
        PacketSystem packetSystem,
        SessionManager sessionManager,
        ServerConnectionService connectionService,
        ICharServerIpcService charServerIpcService,
        ICharServerRegistry charServerRegistry,
        ILoginDataRepository loginDataRepository,
        LoginServerState serverState
    )
        : base("LoginServer", configuration, logger, packetSystem, sessionManager)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
        _handlerRegistry.WarmUpHandlers(TimeSpan.FromSeconds(5), failOnError: false);
        _loginSecurityService = loginSecurityService;
        _charServerIpcService = charServerIpcService;
        _charServerRegistry = charServerRegistry;
        _loginDataRepository = loginDataRepository;
        _charServerLivenessMonitor = new EndpointLivenessMonitor(
            logger,
            scope: "LoginServer -> CharServers",
            probeInterval: TimeSpan.FromSeconds(15),
            connectTimeout: TimeSpan.FromSeconds(1),
            failureThreshold: 3);
        _serverState = serverState;

        // Wire up the connection service to use this server's connection manager
        connectionService.SetConnectionManager(ServerConnections);
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        _serverState.SetState(State);
    }

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await base.StopAsync(cancellationToken);
        _serverState.SetState(State);
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("LoginServer TCP listener started on port {Port}", Configuration.Port);

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

        Logger.LogInformation("LoginServer TCP listener stopped");
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listenerSocket != null)
        {
            try
            {
                var clientSocket = await _listenerSocket.AcceptAsync(cancellationToken);
                var session = SessionManager.CreateSession<LoginSessionData>(clientSocket);

                if (Configuration is LoginServerConfiguration loginConfig &&
                    clientSocket.RemoteEndPoint is IPEndPoint remoteEndPoint &&
                    loginConfig.IpBan &&
                    await _loginSecurityService.IsIpBannedAsync(remoteEndPoint.Address, cancellationToken))
                {
                    await _loginSecurityService.LogLoginAttemptAsync(
                        remoteEndPoint.Address,
                        "unknown",
                        -3,
                        "ip banned",
                        cancellationToken);

                    session.EnqueuePacket(new AC_REFUSE_LOGIN
                    {
                        Error = 3,
                        UnblockTime = string.Empty
                    });
                    await session.FlushPacketsAsync();
                    session.Disconnect(DisconnectReason.Kicked);
                    SessionManager.RemoveSession(session.SessionId);
                    continue;
                }

                Logger.LogInformation(
                    "Client connected: {SessionId} from {RemoteEndpoint}",
                    session.SessionId,
                    clientSocket.RemoteEndPoint);
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
        if (Configuration is LoginServerConfiguration loginConfig &&
            loginConfig.IpBan &&
            loginConfig.IpBanCleanupInterval > 0 &&
            DateTime.UtcNow >= _nextIpBanCleanupUtc)
        {
            _nextIpBanCleanupUtc = DateTime.UtcNow.AddSeconds(loginConfig.IpBanCleanupInterval);
            await _loginSecurityService.CleanupExpiredIpBansAsync(cancellationToken);
        }

        await RequestCharServerAddressSyncAsync(cancellationToken);
        await PruneUnreachableCharServersAsync(cancellationToken);
        await SweepOrphanOnlineEntriesAsync(cancellationToken);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Defensive safety-net sweep — every 10 minutes, walk the online_db
    /// snapshot and drop entries whose char-server id is no longer in
    /// <see cref="ICharServerRegistry"/>. Mirrors rAthena's
    /// <c>login_online_data_cleanup</c> (login.cpp:201). Proactive prune
    /// in <see cref="PruneUnreachableCharServersAsync"/> handles the
    /// common case; this catches drift if a probe is missed or a
    /// registry-removal path skips the online cleanup.
    /// </summary>
    private async Task SweepOrphanOnlineEntriesAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _nextOrphanSweepUtc) return;
        _nextOrphanSweepUtc = DateTime.UtcNow.AddMinutes(10);

        var live = new HashSet<int>(
            _charServerRegistry.GetActiveCharServersWithIds().Select(s => s.ServerId));
        var orphans = _loginDataRepository.SnapshotOnlineEntries()
            .Where(e => !live.Contains(e.CharServer))
            .Select(e => e.AccountId)
            .ToArray();
        if (orphans.Length == 0) return;

        foreach (var accountId in orphans)
        {
            await _loginDataRepository.RemoveOnlineUser(accountId);
        }
        Logger.LogInformation(
            "Online-db orphan sweep removed {Count} entries pointing to dead char servers",
            orphans.Length);
    }

    protected override async Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await session.FlushPacketsAsync();
        }
    }

    private async Task RequestCharServerAddressSyncAsync(CancellationToken cancellationToken)
    {
        // Mirrors rAthena loginchrif.cpp:logchrif_sync_ip_addresses, which
        // runs every login_config.ip_sync_interval minutes (default 10).
        // ip_sync_interval == 0 disables periodic sync entirely.
        var intervalMinutes = (Configuration as LoginServerConfiguration)?.IpSyncInterval ?? 10;
        if (intervalMinutes == 0) return;

        if (DateTime.UtcNow < _nextCharIpSyncUtc)
        {
            return;
        }

        _nextCharIpSyncUtc = DateTime.UtcNow.AddMinutes(intervalMinutes);
        await _charServerIpcService.RequestCharServerAddressSyncAsync(cancellationToken);
    }

    private async Task PruneUnreachableCharServersAsync(CancellationToken cancellationToken)
    {
        var activeServers = _charServerRegistry
            .GetActiveCharServersWithIds()
            .Select(server => new MonitoredEndpoint(
                server.ServerId,
                server.Data.Ip,
                server.Data.Port,
                server.Data.Name))
            .ToList();

        var unreachableServers = await _charServerLivenessMonitor.ProbeDueEndpointsAsync(activeServers, cancellationToken);

        foreach (var server in unreachableServers)
        {
            var removed = await _loginDataRepository.RemoveOnlineUsersByCharServer(server.Id);
            _charServerRegistry.RemoveCharServer(server.Id);

            Logger.LogWarning(
                "Pruned unreachable char server id {ServerId} (endpoint {Endpoint}) and removed {RemovedAccounts} online account bindings",
                server.Id,
                EndpointProbe.FormatEndpoint(server.Ip, server.Port),
                removed);
        }
    }
}
