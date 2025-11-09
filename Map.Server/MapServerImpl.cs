using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;
using Microsoft.Extensions.Logging;

namespace Map.Server;

public class MapServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;

    public MapServerImpl(ServerConfiguration configuration, ILogger<MapServerImpl> logger)
        : base("MapServer", configuration, logger)
    {
        _players = new ConcurrentDictionary<long, PlayerEntity>();
        _sessionToCharacter = new ConcurrentDictionary<Guid, long>();
    }

    protected override async Task StartTcpListenerAsync(CancellationToken cancellationToken)
    {
        _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenerSocket.Bind(new IPEndPoint(IPAddress.Any, Configuration.Port));
        _listenerSocket.Listen(Configuration.MaxConnections);

        Logger.LogInformation("MapServer TCP listener started on port {Port}", Configuration.Port);

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

        Logger.LogInformation("MapServer TCP listener stopped");
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
            case PacketIds.EnterMapRequest:
                await HandleEnterMapRequestAsync(session, data);
                break;
            case PacketIds.MoveRequest:
                await HandleMoveRequestAsync(session, data);
                break;
            case PacketIds.ChatMessage:
                await HandleChatMessageAsync(session, data);
                break;
            default:
                Logger.LogWarning("Unknown packet ID: 0x{PacketId:X4}", packetId);
                break;
        }
    }

    private async Task HandleEnterMapRequestAsync(ClientSession session, Memory<byte> data)
    {
        var reader = new PacketReader(data.Span);
        var characterId = reader.ReadInt64();
        var mapId = reader.ReadUInt32();

        Logger.LogInformation("Character {CharId} entering map {MapId}", characterId, mapId);

        // Create player entity
        var player = new PlayerEntity
        {
            CharacterId = characterId,
            MapId = mapId,
            PositionX = 100.0f,
            PositionY = 0.0f,
            PositionZ = 100.0f,
            SessionId = session.SessionId
        };

        _players[characterId] = player;
        _sessionToCharacter[session.SessionId] = characterId;

        // Send response
        var responsePacket = PacketWriter.CreatePacket(PacketIds.EnterMapResponse, writer =>
        {
            writer.WriteByte(1); // Success
            writer.WriteUInt32(mapId);
            writer.WriteUInt32((uint)player.PositionX);
            writer.WriteUInt32((uint)player.PositionY);
            writer.WriteUInt32((uint)player.PositionZ);
            
            // Send nearby players
            var nearbyPlayers = _players.Values.Where(p => p.CharacterId != characterId).ToList();
            writer.WriteByte((byte)nearbyPlayers.Count);
            
            foreach (var nearbyPlayer in nearbyPlayers)
            {
                writer.WriteInt64(nearbyPlayer.CharacterId);
                writer.WriteUInt32((uint)nearbyPlayer.PositionX);
                writer.WriteUInt32((uint)nearbyPlayer.PositionY);
                writer.WriteUInt32((uint)nearbyPlayer.PositionZ);
            }
        });

        session.EnqueuePacket(responsePacket);

        // Notify other players
        BroadcastPlayerJoined(player);

        await Task.CompletedTask;
    }

    private async Task HandleMoveRequestAsync(ClientSession session, Memory<byte> data)
    {
        if (!_sessionToCharacter.TryGetValue(session.SessionId, out var characterId))
            return;

        if (!_players.TryGetValue(characterId, out var player))
            return;

        var reader = new PacketReader(data.Span);
        var newX = reader.ReadUInt32();
        var newY = reader.ReadUInt32();
        var newZ = reader.ReadUInt32();

        // Update player position
        player.PositionX = newX;
        player.PositionY = newY;
        player.PositionZ = newZ;

        // Broadcast movement to nearby players
        var movePacket = PacketWriter.CreatePacket(PacketIds.MoveResponse, writer =>
        {
            writer.WriteInt64(characterId);
            writer.WriteUInt32(newX);
            writer.WriteUInt32(newY);
            writer.WriteUInt32(newZ);
        });

        BroadcastToMap(player.MapId, movePacket, characterId);

        await Task.CompletedTask;
    }

    private async Task HandleChatMessageAsync(ClientSession session, Memory<byte> data)
    {
        if (!_sessionToCharacter.TryGetValue(session.SessionId, out var characterId))
            return;

        if (!_players.TryGetValue(characterId, out var player))
            return;

        var reader = new PacketReader(data.Span);
        var message = reader.ReadString();

        Logger.LogInformation("Chat from {CharId}: {Message}", characterId, message);

        // Broadcast chat message
        var chatPacket = PacketWriter.CreatePacket(PacketIds.ChatMessage, writer =>
        {
            writer.WriteInt64(characterId);
            writer.WriteString(message);
        });

        BroadcastToMap(player.MapId, chatPacket);

        await Task.CompletedTask;
    }

    private void BroadcastPlayerJoined(PlayerEntity player)
    {
        var packet = PacketWriter.CreatePacket(0x0310, writer =>
        {
            writer.WriteInt64(player.CharacterId);
            writer.WriteUInt32((uint)player.PositionX);
            writer.WriteUInt32((uint)player.PositionY);
            writer.WriteUInt32((uint)player.PositionZ);
        });

        BroadcastToMap(player.MapId, packet, player.CharacterId);
    }

    private void BroadcastToMap(uint mapId, byte[] packet, long? excludeCharacterId = null)
    {
        foreach (var player in _players.Values.Where(p => p.MapId == mapId))
        {
            if (excludeCharacterId.HasValue && player.CharacterId == excludeCharacterId.Value)
                continue;

            var session = SessionManager.GetAllSessions()
                .FirstOrDefault(s => s.SessionId == player.SessionId);
            
            session?.EnqueuePacket(packet);
        }
    }

    protected override async Task UpdateGameLogicAsync(double deltaTime, CancellationToken cancellationToken)
    {
        // Update game logic (AI, physics, etc.)
        // This runs at 60 FPS for MapServer
        
        // Example: Update NPC AI, check collisions, etc.
        // For now, just a placeholder
        
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

public class PlayerEntity
{
    public long CharacterId { get; set; }
    public uint MapId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public Guid SessionId { get; set; }
}

