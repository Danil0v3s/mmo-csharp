using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.World.MapOps;

/// <summary>Default <see cref="IMapOpsService"/>. Shells route through IMapWorldRegistry / IEntityRegistry when wired.</summary>
public sealed class MapOpsService : IMapOpsService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<MapOpsService> _logger;

    public MapOpsService(IEntityRegistry entities, ILogger<MapOpsService> logger)
    {
        _entities = entities;
        _logger = logger;
    }

    public int Name2MapId(string mapName) => (int)(uint)mapName.GetHashCode();
    public string MapId2Name(int mapId) => "";
    public bool RandomCell(string mapName, out short x, out short y, byte flag) { x = 0; y = 0; return false; }
    public bool SearchFreeCell(string mapName, short xCenter, short yCenter, short range, out short x, out short y, byte flag)
    { x = xCenter; y = yCenter; return true; }
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
        // The PC's char-server-side char id isn't exposed on PlayerEntity yet;
        // a CharId column on PlayerEntity is on the porting backlog. For now
        // we degrade to entity-id matching (works for engine-internal callers).
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
    public bool GetCell(int mapId, short x, short y, int cellChk) => true;
    public void SetCell(int mapId, short x, short y, int cellChk, bool set) { }
    public long GetMapFlag(int mapId, int flag) => 0;
    public void SetMapFlag(int mapId, int flag, long value) { }
    public int AddNpc(int mapId, NpcEntity npc) => 0;
    public void Init() { }
    public void Final() { }
    public void Reload() { }
}
