using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _packetHandlers;

    public CharServerImpl(ServerConfiguration configuration, ILogger<CharServerImpl> logger)
        : base("CharServer", configuration, logger)
    {
        _packetHandlers = new ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>>();
        RegisterPacketHandlers();
    }

    private void RegisterPacketHandlers()
    {
        _packetHandlers[PacketHeader.CH_CHARLIST_REQ] = async (session, packet) => 
            await HandleCharacterListRequest(session, (CZ_HEARTBEAT)packet);
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Char Server", _packetHandlers.Count);
    }

    private async Task HandleCharacterListRequest(ClientSession session, CZ_HEARTBEAT packet)
    {
        Logger.LogInformation("Character list request from session {SessionId}", session.SessionId);

        // TODO: Query characters from database
        // For now, return mock data using HC_CHARACTER_LIST
        var responsePacket = new HC_CHARACTER_LIST
        {
            Characters = new[]
            {
                new Core.Server.Packets.ServerPackets.CharacterInfo
                {
                    CharId = 1001,
                    Name = "Warrior123",
                    Exp = 50000,
                    Zeny = 10000,
                    JobLevel = 50
                },
                new Core.Server.Packets.ServerPackets.CharacterInfo
                {
                    CharId = 1002,
                    Name = "Mage456",
                    Exp = 45000,
                    Zeny = 8000,
                    JobLevel = 45
                }
            }
        };

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
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

