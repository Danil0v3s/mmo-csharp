using Map.Server.World;

namespace Map.Server.Entities;

/// <summary>
/// Authoritative registry of every live entity on this map server, plus the
/// per-map spatial index that powers <c>ForEachInRange</c> visibility queries.
///
/// Single source of truth replacing the legacy <c>IPlayerMapService</c> for
/// new code paths (gameplay handlers, visibility broadcasts). The legacy
/// service is implemented as a facade over this registry for IPC code paths
/// that haven't been rewritten yet.
/// </summary>
public interface IEntityRegistry
{
    /// <summary>Insert a new entity. Caller must not insert the same id twice.</summary>
    void Add(Entity entity);

    /// <summary>Remove by id. Returns the removed entity, or null if absent.</summary>
    Entity? Remove(EntityId id);

    /// <summary>Look up by id; null if absent.</summary>
    Entity? Get(EntityId id);

    /// <summary>True iff the registry currently holds an entity with this id.</summary>
    bool Contains(EntityId id);

    /// <summary>
    /// Move an already-registered entity to a new cell. Updates the entity's
    /// position and rebinds it in the spatial index. The map id must match the
    /// entity's current map; cross-map moves go through <see cref="Remove"/>
    /// then <see cref="Add"/>.
    /// </summary>
    void Move(EntityId id, short newX, short newY);

    /// <summary>
    /// Iterate entities within ±range of (cx, cy) on the given map, filtered
    /// by type mask. Returns a snapshot list — safe to mutate the registry
    /// during enumeration.
    /// </summary>
    IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask);

    /// <summary>Iterate entities inside the rectangle, filtered by type mask.</summary>
    IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask);

    /// <summary>Every entity on the registry (any map, any type).</summary>
    IEnumerable<Entity> All();

    /// <summary>Entity count.</summary>
    int Count { get; }
}
