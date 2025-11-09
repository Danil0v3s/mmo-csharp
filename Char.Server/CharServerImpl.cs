using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.IPC;
using Core.Server.Network;
using Microsoft.Extensions.Logging;

namespace Char.Server;

public class CharServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;

    public CharServerImpl(ServerConfiguration configuration, ILogger<CharServerImpl> logger)
        : base("CharServer", configuration, logger)
    {
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
            case PacketIds.CharListRequest:
                await HandleCharListRequestAsync(session, data);
                break;
            case PacketIds.CharCreateRequest:
                await HandleCharCreateRequestAsync(session, data);
                break;
            case PacketIds.CharSelectRequest:
                await HandleCharSelectRequestAsync(session, data);
                break;
            default:
                Logger.LogWarning("Unknown packet ID: 0x{PacketId:X4}", packetId);
                break;
        }
    }

    private async Task HandleCharListRequestAsync(ClientSession session, Memory<byte> data)
    {
        var reader = new PacketReader(data.Span);
        var sessionToken = reader.ReadString();

        Logger.LogInformation("Character list request from session {SessionId}", session.SessionId);

        // Validate session with LoginServer via gRPC
        var loginChannel = IpcClient.GetChannel("LoginServer");
        if (loginChannel != null)
        {
            var loginClient = new LoginService.LoginServiceClient(loginChannel);
            var validationResponse = await loginClient.ValidateSessionAsync(
                new ValidateSessionRequest { SessionToken = sessionToken });

            if (!validationResponse.IsValid)
            {
                Logger.LogWarning("Invalid session token from session {SessionId}", session.SessionId);
                session.Disconnect(DisconnectReason.Kicked);
                return;
            }

            // TODO: Query characters from database
            // For now, return mock data
            var responsePacket = PacketWriter.CreatePacket(PacketIds.CharListResponse, writer =>
            {
                writer.WriteByte(2); // Character count
                
                // Character 1
                writer.WriteInt64(1001);
                writer.WriteString("Warrior123");
                writer.WriteUInt16(50);
                writer.WriteByte(1);
                
                // Character 2
                writer.WriteInt64(1002);
                writer.WriteString("Mage456");
                writer.WriteUInt16(45);
                writer.WriteByte(2);
            });

            session.EnqueuePacket(responsePacket);
        }

        await Task.CompletedTask;
    }

    private async Task HandleCharCreateRequestAsync(ClientSession session, Memory<byte> data)
    {
        var reader = new PacketReader(data.Span);
        var sessionToken = reader.ReadString();
        var characterName = reader.ReadString();
        var classId = reader.ReadByte();

        Logger.LogInformation("Character create request: {CharName}, Class: {ClassId}", 
            characterName, classId);

        // TODO: Create character in database
        var success = true;
        var characterId = (long)new Random().Next(10000, 99999);

        var responsePacket = PacketWriter.CreatePacket(PacketIds.CharCreateResponse, writer =>
        {
            writer.WriteByte((byte)(success ? 1 : 0));
            if (success)
            {
                writer.WriteInt64(characterId);
                writer.WriteString(characterName);
                writer.WriteUInt16(1); // Level
                writer.WriteByte(classId);
            }
            else
            {
                writer.WriteString("Character creation failed");
            }
        });

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
    }

    private async Task HandleCharSelectRequestAsync(ClientSession session, Memory<byte> data)
    {
        var reader = new PacketReader(data.Span);
        var characterId = reader.ReadInt64();

        Logger.LogInformation("Character select request: {CharId}", characterId);

        // TODO: Load character data and prepare for map server
        var mapServerEndpoint = "localhost:5003";

        var responsePacket = PacketWriter.CreatePacket(PacketIds.CharSelectResponse, writer =>
        {
            writer.WriteByte(1); // Success
            writer.WriteString(mapServerEndpoint);
            writer.WriteInt64(characterId);
        });

        session.EnqueuePacket(responsePacket);

        await Task.CompletedTask;
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

