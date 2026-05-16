using Core.Server.Packets;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;

namespace Map.Server.Visibility;

/// <summary>
/// Translates gameplay events (spawn, walk, vanish) into per-recipient packet
/// broadcasts, scoped to the AOI around an entity. Replaces rAthena's
/// <c>clif_send</c> / <c>clif_spawn</c> / <c>clif_clearunit_area</c> trio.
///
/// The service knows nothing about gameplay state beyond what the registry
/// exposes; callers (movement, session, MS2 spawns) decide when an event
/// occurred and what packet to send.
/// </summary>
public interface IVisibilityService
{
    /// <summary>Enqueue a packet on the player's own session. No-op if the session is gone.</summary>
    void SendToSelf(PlayerEntity player, OutgoingPacket packet);

    /// <summary>Enqueue a packet on every PC session whose entity is in view of <paramref name="src"/>.</summary>
    void SendToArea(Entity src, OutgoingPacket packet, SendTarget target = SendTarget.Area);

    /// <summary>
    /// Entities in <paramref name="mask"/> on <paramref name="mapId"/> that
    /// are visible from (toX, toY) but were not visible from (fromX, fromY).
    /// Used by movement to drive "newly entered view" packets.
    /// </summary>
    IReadOnlyList<Entity> NewlyVisible(
        uint mapId,
        short fromX, short fromY,
        short toX, short toY,
        EntityType mask);

    /// <summary>
    /// Entities in <paramref name="mask"/> on <paramref name="mapId"/> that
    /// were visible from (fromX, fromY) but are no longer visible from
    /// (toX, toY). Used by movement to drive "newly left view" packets.
    /// </summary>
    IReadOnlyList<Entity> NewlyInvisible(
        uint mapId,
        short fromX, short fromY,
        short toX, short toY,
        EntityType mask);

    /// <summary>
    /// Build a <see cref="ZC_NOTIFY_STANDENTRY"/> for <paramref name="entered"/>
    /// and enqueue it on every PC currently in view (excluding the entered
    /// entity itself if it's a PC). MS1: PC only — non-PC entities throw.
    /// </summary>
    void NotifySpawnedToArea(Entity entered);

    /// <summary>
    /// Build a <see cref="ZC_NOTIFY_VANISH"/> for <paramref name="gone"/> and
    /// enqueue it on every PC currently in view (excluding <paramref name="gone"/>
    /// itself if it's a PC).
    /// </summary>
    void NotifyVanishedToArea(Entity gone, VanishReason reason);

    /// <summary>
    /// Build a <see cref="ZC_NOTIFY_MOVE"/> for <paramref name="walker"/> and
    /// enqueue it on every PC in view except the walker.
    /// </summary>
    void NotifyMoveToArea(
        Entity walker,
        short fromX, short fromY,
        short toX, short toY,
        uint startTime);

    /// <summary>
    /// On initial spawn, send a <see cref="ZC_NOTIFY_STANDENTRY"/> for every
    /// entity already in view to <paramref name="self"/>'s session. MS1 only
    /// emits packets for PC viewers; mob/npc entries are skipped (they land
    /// in MS2 with their own packet shapes).
    /// </summary>
    void SendCurrentViewToSelf(PlayerEntity self);
}
