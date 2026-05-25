using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.World.MapOps;

/// <summary>Default <see cref="IMapOpsService"/>. Wave 85 — routed
/// through <see cref="IMapWorldRegistry"/> for cell-flag reads / writes
/// and reverse map-id → name lookup; <see cref="IEntityRegistry"/>
/// powers id / nick / charid lookups + foreach iterations.</summary>
public sealed class MapOpsService : IMapOpsService
{
    private readonly IEntityRegistry _entities;
    private readonly IMapWorldRegistry? _world;
    private readonly ILogger<MapOpsService> _logger;
    private static readonly Random Rng = new();

    public MapOpsService(IEntityRegistry entities, ILogger<MapOpsService> logger,
        IMapWorldRegistry? world = null)
    {
        _entities = entities;
        _world = world;
        _logger = logger;
    }

    public int Name2MapId(string mapName) => (int)(uint)mapName.GetHashCode();

    /// <summary>
    /// rAthena <c>map_mapid2mapname</c>. Walks the world registry to
    /// find the map whose hashed name matches the id. Returns empty
    /// when the registry is absent or no match (matches rAthena's
    /// "" fallback for unknown ids).
    /// </summary>
    public string MapId2Name(int mapId)
    {
        if (_world == null) return "";
        foreach (var map in _world.All)
        {
            if ((uint)map.Name.GetHashCode() == (uint)mapId) return map.Name;
        }
        return "";
    }

    /// <summary>
    /// rAthena <c>map_random_cell</c>. Picks a random walkable cell on
    /// the map. Tries up to 100 random (x, y) draws; falls back to
    /// (0, 0) with false on failure (matches rAthena's exhaustion path).
    /// </summary>
    public bool RandomCell(string mapName, out short x, out short y, byte flag)
    {
        x = 0; y = 0;
        if (_world == null) return false;
        var map = _world.Get(mapName);
        if (map == null) return false;
        for (var i = 0; i < 100; i++)
        {
            var rx = (short)Rng.Next(0, map.Xs);
            var ry = (short)Rng.Next(0, map.Ys);
            if (map.IsWalkable(rx, ry))
            {
                x = rx; y = ry; return true;
            }
        }
        return false;
    }

    /// <summary>
    /// rAthena <c>map_search_freecell</c>. Square outward scan from
    /// (<paramref name="xCenter"/>, <paramref name="yCenter"/>) up to
    /// <paramref name="range"/> cells, returning the first walkable
    /// cell. Falls back to the input cell when nothing in range is
    /// walkable.
    /// </summary>
    public bool SearchFreeCell(string mapName, short xCenter, short yCenter, short range, out short x, out short y, byte flag)
    {
        x = xCenter; y = yCenter;
        if (_world == null) return true; // can't disprove without map
        var map = _world.Get(mapName);
        if (map == null) return true;
        if (map.IsWalkable(xCenter, yCenter)) return true;
        for (short r = 1; r <= range; r++)
        {
            for (short dx = (short)-r; dx <= r; dx++)
            {
                for (short dy = (short)-r; dy <= r; dy++)
                {
                    if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue; // edge of ring
                    var nx = (short)(xCenter + dx);
                    var ny = (short)(yCenter + dy);
                    if (map.IsWalkable(nx, ny))
                    {
                        x = nx; y = ny; return true;
                    }
                }
            }
        }
        return false;
    }
    public int CheckDir(byte dir, byte targetDir) => (dir - targetDir + 8) % 8;
    public byte CalcDir(short srcX, short srcY, short dstX, short dstY)
    {
        var dx = dstX - srcX; var dy = dstY - srcY;
        if (dx == 0 && dy > 0) return 0;
        if (dx > 0 && dy > 0) return 1;
        if (dx > 0 && dy == 0) return 2;
        if (dx > 0 && dy < 0) return 3;
        if (dx == 0 && dy < 0) return 4;
        if (dx < 0 && dy < 0) return 5;
        if (dx < 0 && dy == 0) return 6;
        return 7;
    }
    public int AddBlock(Entity bl) => 0;
    public int DelBlock(Entity bl) => 0;
    public int MoveBlock(Entity bl, short x, short y, long tick) { _entities.Move(bl.Id, x, y); return 0; }
    public void AddIdDb(int id, Entity bl) { }
    public void DelIdDb(int id) { }
    public Entity? Id2Bl(int id) => _entities.Get(new EntityId(id));
    public PlayerEntity? CharId2Sd(int charId)
    {
        // Wave 85 — `PlayerEntity.CharacterId == Id.Value` per the
        // rAthena convention (per the PlayerEntity comment block: "EntityId
        // == CharacterId for PCs"). Use the registry's direct lookup
        // rather than scanning All().
        var ent = _entities.Get(new EntityId(charId));
        return ent as PlayerEntity;
    }
    public PlayerEntity? Nick2Sd(string name) =>
        _entities.All().OfType<PlayerEntity>().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    public int ForeachPc(Action<PlayerEntity> action)
    {
        var n = 0;
        foreach (var e in _entities.All()) if (e is PlayerEntity p) { action(p); n++; }
        return n;
    }
    public int ForeachMob(Action<MobEntity> action)
    {
        var n = 0;
        foreach (var e in _entities.All()) if (e is MobEntity m) { action(m); n++; }
        return n;
    }
    public int ForeachInMap(int mapId, Action<Entity> action)
    {
        var n = 0;
        foreach (var e in _entities.All()) if (e.MapId == (uint)mapId) { action(e); n++; }
        return n;
    }
    public int ForeachInRange(int mapId, short cx, short cy, short range, Action<Entity> action)
    {
        var n = 0;
        foreach (var e in _entities.ForEachInRange((uint)mapId, cx, cy, range, EntityType.All))
        {
            action(e); n++;
        }
        return n;
    }
    /// <summary>
    /// rAthena <c>map_getcell</c> (map.cpp:1450). Reads the cell-flag
    /// bitset on <paramref name="mapId"/>, <paramref name="x"/>,
    /// <paramref name="y"/> and tests whether the requested
    /// <paramref name="cellChk"/> flag is set. The <c>cellChk</c>
    /// integer is the rAthena <c>CELL_CHK*</c> constant; we map onto
    /// our <see cref="CellFlags"/> bitset:
    ///   0 = WALKABLE, 1 = SHOOTABLE, 2 = WATER, 5 = NPC_TRIGGER.
    /// Any cellChk value outside the mapped set returns true (rAthena
    /// returns false; we err on the safe side until the full constant
    /// table ports).
    /// </summary>
    public bool GetCell(int mapId, short x, short y, int cellChk)
    {
        var map = ResolveMap(mapId);
        if (map == null) return true;
        var flags = map.GetCell(x, y);
        return cellChk switch
        {
            0 => (flags & CellFlags.Walkable) != 0,
            1 => (flags & CellFlags.Shootable) != 0,
            2 => (flags & CellFlags.Water) != 0,
            5 => (flags & CellFlags.NpcTrigger) != 0,
            _ => true,
        };
    }

    /// <summary>
    /// rAthena <c>map_setcell</c> (map.cpp:1496). Toggles the dynamic
    /// cell flag at (<paramref name="x"/>, <paramref name="y"/>).
    /// Routes to <see cref="MapData.SetDynamicFlag"/>. The fixed
    /// terrain bits (Walkable/Shootable/Water from .gat) are
    /// immutable — only the dynamic layer (NPC_TRIGGER etc) accepts
    /// SetCell mutation.
    /// </summary>
    public void SetCell(int mapId, short x, short y, int cellChk, bool set)
    {
        var map = ResolveMap(mapId);
        if (map == null) return;
        var flag = cellChk switch
        {
            5 => CellFlags.NpcTrigger,
            _ => CellFlags.None,
        };
        if (flag == CellFlags.None) return;
        map.SetDynamicFlag(x, y, flag, set);
    }

    private MapData? ResolveMap(int mapId)
    {
        if (_world == null) return null;
        var u = (uint)mapId;
        foreach (var map in _world.All)
        {
            if ((uint)map.Name.GetHashCode() == u) return map;
        }
        return null;
    }
    public long GetMapFlag(int mapId, int flag) => 0;
    public void SetMapFlag(int mapId, int flag, long value) { }
    public int AddNpc(int mapId, NpcEntity npc) => 0;
    public void Init() { }
    public void Final() { }
    public void Reload() { }
}
