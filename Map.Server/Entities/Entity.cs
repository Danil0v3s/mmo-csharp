namespace Map.Server.Entities;

/// <summary>
/// Abstract base for every entity that lives on a map. Mirrors rAthena's
/// <c>struct block_list</c> (map.hpp). The fields here are the hot path for
/// the spatial index and visibility checks; subclasses add per-type state.
///
/// Coordinates are cell-based <see cref="short"/> values matching the wire
/// protocol; direction is the rAthena 8-direction enum (0=N, 1=NE, …, 7=NW).
/// </summary>
public abstract class Entity
{
    public EntityId Id { get; }
    public abstract EntityType Type { get; }
    public uint MapId { get; internal set; }
    public short X { get; internal set; }
    public short Y { get; internal set; }
    public byte Dir { get; internal set; }

    protected Entity(EntityId id, uint mapId, short x, short y)
    {
        Id = id;
        MapId = mapId;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Update the entity's cell position. Callers must also notify the
    /// spatial index (via <c>IEntityRegistry.Move</c>) for the bucket
    /// rebinding to happen.
    /// </summary>
    internal void SetPosition(uint mapId, short x, short y)
    {
        MapId = mapId;
        X = x;
        Y = y;
    }
}
