using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;
using Microsoft.Extensions.Logging;

namespace Login.Server;

public class LoginServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;

    public LoginServerImpl(ServerConfiguration configuration, ILogger<LoginServerImpl> logger)
        : base("LoginServer", configuration, logger)
    {
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
            while (session.IncomingPackets.TryDequeue(out var packet))
            {
                try
                {
                    await HandlePacketAsync(session, packet.PacketId, packet.Data);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error handling packet {PacketId} from session {SessionId}",
                        packet.PacketId, session.SessionId);
                }
            }
        }
    }

    private async Task HandlePacketAsync(ClientSession session, ushort packetId, Memory<byte> data)
    {
        switch (packetId)
        {
            case PacketIds.LoginRequest:
                await HandleLoginRequestAsync(session, data);
                break;
            default:
                Logger.LogWarning("Unknown packet ID: 0x{PacketId:X4}", packetId);
                break;
        }
    }

    private async Task HandleLoginRequestAsync(ClientSession session, Memory<byte> data)
    {
        var reader = new PacketReader(data.Span);
        var username = reader.ReadString();
        var password = reader.ReadString();

        Logger.LogInformation("Login request from session {SessionId}: {Username}", 
            session.SessionId, username);

        // TODO: Validate credentials against database
        // For now, accept any login
        var success = true;
        var accountId = (long)new Random().Next(1000, 9999);
        var sessionToken = Guid.NewGuid().ToString();

        var responsePacket = PacketWriter.CreatePacket(PacketIds.LoginResponse, writer =>
        {
            writer.WriteByte((byte)(success ? 1 : 0));
            if (success)
            {
                writer.WriteInt64(accountId);
                writer.WriteString(sessionToken);
            }
            else
            {
                writer.WriteString("Invalid credentials");
            }
        });

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
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

