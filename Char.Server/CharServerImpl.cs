using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Char.Server;

public class CharServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly CharPacketHandler _packetHandler;

    public CharServerImpl(ServerConfiguration configuration, ILogger<CharServerImpl> logger)
        : base("CharServer", configuration, logger)
    {
        _packetHandler = new CharPacketHandler(logger as ILogger);
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
            await _packetHandler.ProcessSessionPacketsAsync(session);
        }
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // Char server doesn't have much game logic
        await Task.CompletedTask;
    }

    protected override async Task FlushOutgoingPacketsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in SessionManager.GetAllSessions())
        {
            await session.FlushPacketsAsync();
        }
    }
}

