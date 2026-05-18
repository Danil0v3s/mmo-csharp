using Map.Server.Entities;
using Map.Server.World;

namespace Map.Server.Scripting;

public sealed class NpcSpawnService : INpcSpawnService
{
    private readonly INpcRegistry _registry;
    private readonly IEntityRegistry _entities;
    private readonly IMapWorldRegistry _world;
    private readonly EntityIdAllocator _idAllocator;
    private readonly ILogger<NpcSpawnService> _logger;

    public int SpawnedCount { get; private set; }
    public int SkippedUnknownMapCount { get; private set; }

    public NpcSpawnService(
        INpcRegistry registry,
        IEntityRegistry entities,
        IMapWorldRegistry world,
        EntityIdAllocator idAllocator,
        ILogger<NpcSpawnService> logger)
    {
        _registry = registry;
        _entities = entities;
        _world = world;
        _idAllocator = idAllocator;
        _logger = logger;
    }

    public void SpawnInitial()
    {
        foreach (var reg in _registry.AllNpcs())
        {
            var map = _world.Get(reg.Map);
            if (map == null)
            {
                _logger.LogDebug(
                    "NPC '{Name}' targets unhosted map '{Map}' — skipped",
                    reg.Name, reg.Map);
                SkippedUnknownMapCount++;
                continue;
            }

            // mapId convention matches MobSpawnService / WarpService / EntityRegistry:
            // uint hash of the map name. Once IMapWorldRegistry exposes a real numeric
            // id, swap this for the proper lookup.
            var mapId = (uint)map.Name.GetHashCode();

            var entity = new NpcEntity(
                id: _idAllocator.NextNpc(),
                name: reg.Name,
                spriteId: reg.Sprite,
                mapId: mapId,
                x: reg.X,
                y: reg.Y,
                dir: reg.Dir,
                triggerArea: reg.TriggerArea,
                hooks: reg.Hooks);

            _entities.Add(entity);
            SpawnedCount++;
        }

        if (SpawnedCount > 0 || SkippedUnknownMapCount > 0)
        {
            _logger.LogInformation(
                "Placed {Spawned} NPCs from script registry; skipped {Skipped} on unhosted maps",
                SpawnedCount, SkippedUnknownMapCount);
        }
    }
}
