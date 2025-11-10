using System.Collections.Concurrent;
using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.ClientPackets;
using Core.Server.Packets.ServerPackets;
using Microsoft.Extensions.Logging;

namespace Map.Server;

/// <summary>
/// Handles all incoming packets for the Map Server.
/// </summary>
public class MapPacketHandler : PacketHandler
{
    private readonly ConcurrentDictionary<long, PlayerEntity> _players;
    private readonly ConcurrentDictionary<Guid, long> _sessionToCharacter;
    private readonly SessionManager _sessionManager;

    public MapPacketHandler(
        ILogger logger,
        ConcurrentDictionary<long, PlayerEntity> players,
        ConcurrentDictionary<Guid, long> sessionToCharacter,
        SessionManager sessionManager) : base(logger)
    {
        _players = players;
        _sessionToCharacter = sessionToCharacter;
        _sessionManager = sessionManager;
    }

    public override async Task HandlePacketAsync(ClientSession session, IncomingPacket packet)
    {
        switch (packet)
        {
            // TODO: Implement CZ_ENTER, CZ_REQUEST_MOVE, CZ_REQUEST_CHAT packet classes
            // Example structure:
            // case CZ_ENTER enter:
            //     await HandleEnterMapAsync(session, enter);
            //     break;
            //
            // case CZ_REQUEST_MOVE move:
            //     await HandleMoveRequestAsync(session, move);
            //     break;
            //
            // case CZ_REQUEST_CHAT chat:
            //     await HandleChatMessageAsync(session, chat);
            //     break;

            default:
                Logger.LogWarning("Unhandled packet type: {PacketType} (Header: 0x{Header:X4}) from session {SessionId}",
                    packet.GetType().Name, (short)packet.Header, session.SessionId);
                break;
        }

        await Task.CompletedTask;
    }

    private async Task HandleEnterMapAsync(ClientSession session, long characterId, uint mapId)
    {
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
            SendPacket(session, entityListPacket);
        }

        // TODO: Send ZC_ACCEPT_ENTER packet

        // Notify other players
        BroadcastPlayerJoined(player);

        await Task.CompletedTask;
    }

    private async Task HandleMoveRequestAsync(ClientSession session, short x, short y)
    {
        if (!_sessionToCharacter.TryGetValue(session.SessionId, out var characterId))
            return;

        if (!_players.TryGetValue(characterId, out var player))
            return;

        // Update player position
        player.PositionX = x;
        player.PositionZ = y;

        // TODO: Broadcast ZC_NOTIFY_MOVE to nearby players

        await Task.CompletedTask;
    }

    private async Task HandleChatMessageAsync(ClientSession session, string message)
    {
        if (!_sessionToCharacter.TryGetValue(session.SessionId, out var characterId))
            return;

        if (!_players.TryGetValue(characterId, out var player))
            return;

        Logger.LogInformation("Chat from {CharId}: {Message}", characterId, message);

        // TODO: Broadcast ZC_NOTIFY_CHAT to nearby players

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
                SendPacket(otherSession, joinPacket);
            }
        }
    }
}

