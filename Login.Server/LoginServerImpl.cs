using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Login.Server;

public class LoginServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _packetHandlers;

    public LoginServerImpl(ServerConfiguration configuration, ILogger<LoginServerImpl> logger)
        : base("LoginServer", configuration, logger)
    {
        _packetHandlers = new ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>>();
        RegisterPacketHandlers();
    }

    private void RegisterPacketHandlers()
    {
        _packetHandlers[PacketHeader.CA_LOGIN] = async (session, packet) => 
            await HandleLogin(session, (CA_LOGIN)packet);
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Login Server", _packetHandlers.Count);
    }

    private async Task HandleLogin(ClientSession session, CA_LOGIN packet)
    {
        Logger.LogInformation("Login request from session {SessionId}: {Username}",
            session.SessionId, packet.Username);

        // TODO: Validate credentials against database
        // For now, accept any non-empty credentials
        bool success = !string.IsNullOrWhiteSpace(packet.Username) &&
                      !string.IsNullOrWhiteSpace(packet.Password);

        if (success)
        {
            var sessionToken = new Random().Next(100000, 999999);

            var responsePacket = new AC_ACCEPT_LOGIN
            {
                SessionToken = sessionToken,
                CharacterSlots = 9
            };

            session.EnqueuePacket(responsePacket);

            Logger.LogInformation("Login successful for {Username}, token: {Token}",
                packet.Username, sessionToken);
        }
        else
        {
            // TODO: Implement AC_REFUSE_LOGIN packet
            Logger.LogWarning("Login failed for session {SessionId} - invalid credentials", session.SessionId);
            session.Disconnect(DisconnectReason.Kicked);
        }

        await Task.CompletedTask;
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
            await ProcessSessionPacketsAsync(session);
        }
    }

    private async Task ProcessSessionPacketsAsync(ClientSession session)
    {
        while (session.IncomingPackets.TryDequeue(out var packet))
        {
            try
            {
                if (_packetHandlers.TryGetValue(packet.Header, out var handler))
                {
                    await handler(session, packet);
                }
                else
                {
                    Logger.LogError("No handler registered for packet {PacketType} (Header: 0x{Header:X4}) from session {SessionId}. Disconnecting client.",
                        packet.GetType().Name, (short)packet.Header, session.SessionId);
                    session.Disconnect(DisconnectReason.UnhandledPacket);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling packet {PacketType} from session {SessionId}. Disconnecting client.",
                    packet.GetType().Name, session.SessionId);
                session.Disconnect(DisconnectReason.PacketHandlerError);
            }
        }
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // Login server doesn't have much game logic
        // Could implement login rate limiting, session cleanup, etc.
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

