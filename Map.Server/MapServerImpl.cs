using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Core.Server;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Map.Server;

public class MapServerImpl : GameLoopServer
{
    private Socket? _listenerSocket;
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;
    private readonly ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>> _packetHandlers;

    public MapServerImpl(ServerConfiguration configuration, ILogger<MapServerImpl> logger)
        : base("MapServer", configuration, logger)
    {
        _players = new ConcurrentDictionary<long, PlayerEntity>();
        _sessionToCharacter = new ConcurrentDictionary<Guid, long>();
        _packetHandlers = new ConcurrentDictionary<PacketHeader, Func<ClientSession, IncomingPacket, Task>>();
        RegisterPacketHandlers();
    }

    private void RegisterPacketHandlers()
    {
        _packetHandlers[PacketHeader.CZ_ENTER] = async (session, packet) => await HandleEnterMap(session, (CZ_HEARTBEAT)packet);
        
        Logger.LogInformation("Registered {Count} packet handler(s) for Map Server", _packetHandlers.Count);
    }

    private async Task HandleEnterMap(ClientSession session, CZ_HEARTBEAT packet)
    {
        // TODO: Extract characterId and mapId from actual CZ_ENTER packet
        long characterId = 1001; // Placeholder
        uint mapId = 1; // Placeholder

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

        // Get nearby entities
        var nearbyEntities = _players.Values
            .Where(p => p.MapId == mapId && p.CharacterId != characterId)
            .Select(p => new EntityInfo
            {
                EntityId = (int)p.CharacterId,
                X = (short)p.PositionX,
                Y = (short)p.PositionZ,
                EntityType = 1, // Player type
                Name = $"Player{p.CharacterId}"
            })
            .ToArray();

        // Send entity list
        if (nearbyEntities.Length > 0)
        {
            var entityListPacket = new ZC_ENTITY_LIST
            {
                Entities = nearbyEntities
            };
            session.EnqueuePacket(entityListPacket);
        }

        // TODO: Send ZC_ACCEPT_ENTER packet

        // Notify other players
        BroadcastPlayerJoined(player);

        await Task.CompletedTask;
    }

    private void BroadcastPlayerJoined(PlayerEntity player)
    {
        // Create entity info for the joining player
        var entityInfo = new EntityInfo
        {
            EntityId = (int)player.CharacterId,
            X = (short)player.PositionX,
            Y = (short)player.PositionZ,
            EntityType = 1,
            Name = $"Player{player.CharacterId}"
        };

        var joinPacket = new ZC_ENTITY_LIST
        {
            Entities = new[] { entityInfo }
        };

        // Broadcast to all players on the same map
        foreach (var otherPlayer in _players.Values.Where(p => p.MapId == player.MapId && p.CharacterId != player.CharacterId))
        {
            var otherSession = SessionManager.GetAllSessions()
                .FirstOrDefault(s => s.SessionId == otherPlayer.SessionId);

            if (otherSession != null)
            {
                otherSession.EnqueuePacket(joinPacket);
            }
        }
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

