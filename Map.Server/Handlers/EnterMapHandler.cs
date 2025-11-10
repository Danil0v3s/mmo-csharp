using System.Collections.Concurrent;
using Core.Server.Network;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Map.Server.Handlers;

/// <summary>
/// Handles map entry requests.
/// Note: This is a placeholder - actual CZ_ENTER packet class needs to be implemented.
/// </summary>
public class EnterMapHandler : IPacketHandler<CZ_HEARTBEAT>
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;
    private readonly SessionManager _sessionManager;

    public EnterMapHandler(
        ILogger logger,
        ConcurrentDictionary<long, PlayerEntity> players,
        ConcurrentDictionary<Guid, long> sessionToCharacter,
        SessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _players = players ?? throw new ArgumentNullException(nameof(players));
        _sessionToCharacter = sessionToCharacter ?? throw new ArgumentNullException(nameof(sessionToCharacter));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    public async Task HandleAsync(ClientSession session, CZ_HEARTBEAT packet)
    {
        // TODO: Extract characterId and mapId from actual CZ_ENTER packet
        long characterId = 1001; // Placeholder
        uint mapId = 1; // Placeholder

        _logger.LogInformation("Character {CharId} entering map {MapId}", characterId, mapId);

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
            var otherSession = _sessionManager.GetAllSessions()
                .FirstOrDefault(s => s.SessionId == otherPlayer.SessionId);

            if (otherSession != null)
            {
                otherSession.EnqueuePacket(joinPacket);
            }
        }
    }
}

