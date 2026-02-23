using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.Out.AC;
using Login.Server.Security;

namespace Login.Server;

public class LoginServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly PacketHandlerRegistry _handlerRegistry;
    private readonly ILoginSecurityService _loginSecurityService;
    private readonly UseCase.ICharServerIpcService _charServerIpcService;
    private readonly UseCase.LoginServerState _serverState;
    private DateTime _nextIpBanCleanupUtc = DateTime.MinValue;
    private DateTime _nextCharIpSyncUtc = DateTime.MinValue;

    public LoginServerImpl(
        ServerConfiguration configuration,
        ILogger<LoginServerImpl> logger,
        IServiceProvider serviceProvider,
        ILoginSecurityService loginSecurityService,
        PacketSystem packetSystem,
        SessionManager sessionManager,
        ServerConnectionService connectionService,
        UseCase.ICharServerIpcService charServerIpcService,
        UseCase.LoginServerState serverState
    )
        : base("LoginServer", configuration, logger, packetSystem, sessionManager)
    {
        _handlerRegistry = new PacketHandlerRegistry(serviceProvider, logger);
        _handlerRegistry.DiscoverAndRegisterFromCallingAssembly();
        _handlerRegistry.WarmUpHandlers(TimeSpan.FromSeconds(5), failOnError: false);
        _loginSecurityService = loginSecurityService;
        _charServerIpcService = charServerIpcService;
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

        await Task.CompletedTask;
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
        if (DateTime.UtcNow < _nextCharIpSyncUtc)
        {
            return;
        }

        _nextCharIpSyncUtc = DateTime.UtcNow.AddSeconds(60);
        await _charServerIpcService.RequestCharServerAddressSyncAsync(cancellationToken);
    }
}
