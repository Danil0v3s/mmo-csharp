using System.Collections.Concurrent;
using Map.Server.World;

namespace Map.Server.Entities;

public sealed class EntityRegistry : IEntityRegistry
{
    private readonly ConcurrentDictionary<EntityId, Entity> _byId = new();
    // Per-map spatial indexes built lazily from the world registry. Each map's
    // index is sized from MapData.Xs/Ys at construction.
    private readonly ConcurrentDictionary<uint, MapSpatialIndex> _spatial = new();
    private readonly IMapWorldRegistry _worldRegistry;

    public EntityRegistry(IMapWorldRegistry worldRegistry)
    {
        _worldRegistry = worldRegistry;
    }

    public int Count => _byId.Count;

    public void Add(Entity entity)
    {
        if (!_byId.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException(
                $"Entity {entity.Id} already registered (type {entity.Type})");
        }
        GetOrCreateIndex(entity.MapId)?.Insert(entity.Id, entity.X, entity.Y);
    }

    public Entity? Remove(EntityId id)
    {
        if (!_byId.TryRemove(id, out var entity)) return null;
        if (_spatial.TryGetValue(entity.MapId, out var index))
        {
            index.Remove(id, entity.X, entity.Y);
        }
        return entity;
    }

    public Entity? Get(EntityId id) => _byId.GetValueOrDefault(id);

    public bool Contains(EntityId id) => _byId.ContainsKey(id);

    public void Move(EntityId id, short newX, short newY)
    {
        if (!_byId.TryGetValue(id, out var entity))
        {
            throw new InvalidOperationException($"Cannot move unknown entity {id}");
        }
        if (entity.X == newX && entity.Y == newY) return;

        if (_spatial.TryGetValue(entity.MapId, out var index))
        {
            index.Move(id, entity.X, entity.Y, newX, newY);
        }
        entity.SetPosition(entity.MapId, newX, newY);
    }

    public IReadOnlyList<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)
    {
        if (!_spatial.TryGetValue(mapId, out var index)) return Array.Empty<Entity>();
        var ids = index.ForEachInRange(cx, cy, range);
        return FilterByMask(ids, mask);
    }

    public IReadOnlyList<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)
    {
        if (!_spatial.TryGetValue(mapId, out var index)) return Array.Empty<Entity>();
        var ids = index.ForEachInArea(x0, y0, x1, y1);
        return FilterByMask(ids, mask);
    }

    public IEnumerable<Entity> All() => _byId.Values;

    private IReadOnlyList<Entity> FilterByMask(List<EntityId> ids, EntityType mask)
    {
        if (ids.Count == 0) return Array.Empty<Entity>();
        var result = new List<Entity>(ids.Count);
        foreach (var id in ids)
        {
            if (_byId.TryGetValue(id, out var entity) && (entity.Type & mask) != 0)
            {
                result.Add(entity);
            }
        }
        return result;
    }

    private MapSpatialIndex? GetOrCreateIndex(uint mapId)
    {
        if (_spatial.TryGetValue(mapId, out var existing)) return existing;
        // mapId is the registry's internal numeric id; in MS1 we use the
        // index into IMapWorldRegistry.All. Until a proper MapIndex lookup
        // lands, we resolve by enumeration — cheap for the small map count.
        // Once MapIndex (planned in world.md Pending) lands, switch to O(1).
        foreach (var map in _worldRegistry.All)
        {
            // Treat MapData identity by name hash for now — registry doesn't
            // yet assign numeric ids to maps. Callers in MS1 will pass mapId
            // derived the same way (or 0 placeholder); update this when
            // MapIndex arrives.
            var hash = (uint)map.Name.GetHashCode();
            if (hash == mapId)
            {
                return _spatial.GetOrAdd(mapId, _ => new MapSpatialIndex(map.Xs, map.Ys));
            }
        }
        // Map not in world registry — entity insertion is allowed (entity is
        // still tracked by id) but it doesn't participate in spatial queries.
        return null;
    }
}
