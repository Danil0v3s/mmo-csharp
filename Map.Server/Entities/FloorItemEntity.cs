namespace Map.Server.Entities;

/// <summary>
/// A floor-item instance: stack of items sitting on a cell, waiting to be
/// picked up or despawned. Mirrors rAthena's <c>struct flooritem_data</c>
/// (map.hpp). EntityId is allocated from <see cref="EntityIdAllocator"/>'s
/// item range; lookups by block-list id match the client's protocol.
///
/// MS3 first slice: enough state for drop / pickup / despawn. Loot-protection
/// owners, drop options, and bound/refined attributes come with the full
/// inventory model.
/// </summary>
public sealed class FloorItemEntity : Entity
{
    public int ItemId { get; }
    public short Amount { get; private set; }
    public byte Identified { get; }
    public byte SubX { get; }
    public byte SubY { get; }

    /// <summary>
    /// <see cref="Environment.TickCount64"/> at drop time. Per-tick despawn
    /// sweep removes items older than the configured TTL.
    /// </summary>
    public long DroppedAtTick { get; }

    public override EntityType Type => EntityType.Item;

    public FloorItemEntity(
        EntityId id,
        int itemId,
        short amount,
        uint mapId,
        short x,
        short y,
        byte subX,
        byte subY,
        long droppedAtTick,
        byte identified = 1)
        : base(id, mapId, x, y)
    {
        ItemId = itemId;
        Amount = amount;
        Identified = identified;
        SubX = subX;
        SubY = subY;
        DroppedAtTick = droppedAtTick;
    }
}
