using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Visibility;

namespace Map.Server.Handlers;

/// <summary>
/// "LoadEndAck" — the client tells us it's finished loading the map data
/// and is ready to render entities. rAthena <c>clif_parse_LoadEndAck</c>:
/// flips the session from auth-only to actually spawned, registers the
/// <see cref="PlayerEntity"/> in the spatial index, and emits the entry +
/// surroundings packets so the world appears around the player.
/// </summary>
[PacketHandler(PacketHeader.CZ_NOTIFY_ACTORINIT)]
public class NotifyActorInitHandler(
    IEntityRegistry registry,
    IVisibilityService visibility,
    ILogger<NotifyActorInitHandler> logger
) : IPacketHandler<MapSessionData, CZ_NOTIFY_ACTORINIT>
{
    public Task HandleAsync(MapSessionData session, CZ_NOTIFY_ACTORINIT packet)
    {
        if (session.AuthState != MapAuthState.Authenticated)
        {
            logger.LogWarning(
                "CZ_NOTIFY_ACTORINIT on session {SessionId} in unexpected state {State} — ignoring",
                session.SessionId, session.AuthState);
            return Task.CompletedTask;
        }

        if (session.CharacterId is not { } charId
            || session.AccountId is not { } accountId
            || session.MapId is not { } mapId)
        {
            logger.LogError("Session {SessionId} reached actor-init without bound character info", session.SessionId);
            session.Disconnect(DisconnectReason.PacketHandlerError);
            return Task.CompletedTask;
        }

        // Defensive: if a stale entity exists for this char_id (crash recovery
        // / gRPC EnterMap placeholder), tear it down before re-spawning.
        if (registry.Get(new EntityId(charId)) != null)
        {
            registry.Remove(new EntityId(charId));
        }

        var player = new PlayerEntity(
            characterId: charId,
            accountId: accountId,
            name: session.CharacterName ?? string.Empty,
            sessionId: session.SessionId,
            mapId: mapId,
            x: session.SpawnX,
            y: session.SpawnY);
        player.Dir = session.SpawnDir;
        registry.Add(player);

        session.EntityId = player.Id;
        session.AuthState = MapAuthState.Spawned;

        // Tell other players in view about us; then tell us about them.
        visibility.NotifySpawnedToArea(player);
        visibility.SendCurrentViewToSelf(player);

        logger.LogInformation(
            "Player {Name} (char {CharId}) spawned at ({X},{Y}) on map 0x{MapId:X8}",
            player.Name, charId, player.X, player.Y, mapId);

        return Task.CompletedTask;
    }
}
