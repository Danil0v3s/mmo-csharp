using Map.Server.Entities;

namespace Map.Server.World.MapOps;

/// <summary>
/// Map-level utility operations. Canonical entry points for the
/// rAthena <c>map.cpp</c> public surface (5 356 lines, 157 functions).
///
/// The C# port already has a working `IMapWorldRegistry` covering
/// the per-map metadata + cell flags + entity index. This service
/// surfaces the *operation-flavored* rAthena entry points
/// (foreach_in_*, addiddb, deliddb, get_random_*, mapindex, …).
/// </summary>
public interface IMapOpsService
{
    /// <summary>rAthena <c>map_mapname2mapid</c>.</summary>
    int Name2MapId(string mapName);

    /// <summary>rAthena <c>map_mapid2mapname</c>.</summary>
    string MapId2Name(int mapId);

    /// <summary>rAthena <c>map_random_cell</c>.</summary>
    bool RandomCell(string mapName, out short x, out short y, byte flag);

    /// <summary>rAthena <c>map_search_freecell</c>.</summary>
    bool SearchFreeCell(string mapName, short xCenter, short yCenter, short range, out short x, out short y, byte flag);

    /// <summary>rAthena <c>map_check_dir</c>.</summary>
    int CheckDir(byte dir, byte targetDir);

    /// <summary>rAthena <c>map_calc_dir</c>.</summary>
    byte CalcDir(short srcX, short srcY, short dstX, short dstY);

    /// <summary>rAthena <c>map_addblock</c>.</summary>
    int AddBlock(Entity bl);

    /// <summary>rAthena <c>map_delblock</c>.</summary>
    int DelBlock(Entity bl);

    /// <summary>rAthena <c>map_moveblock</c>.</summary>
    int MoveBlock(Entity bl, short x, short y, long tick);

    /// <summary>rAthena <c>map_addiddb</c>.</summary>
    void AddIdDb(int id, Entity bl);

    /// <summary>rAthena <c>map_deliddb</c>.</summary>
    void DelIdDb(int id);

    /// <summary>rAthena <c>map_id2bl</c>.</summary>
    Entity? Id2Bl(int id);

    /// <summary>rAthena <c>map_charid2sd</c>.</summary>
    PlayerEntity? CharId2Sd(int charId);

    /// <summary>rAthena <c>map_nick2sd</c>.</summary>
    PlayerEntity? Nick2Sd(string name);

    /// <summary>rAthena <c>map_foreachpc</c>.</summary>
    int ForeachPc(Action<PlayerEntity> action);

    /// <summary>rAthena <c>map_foreachmob</c>.</summary>
    int ForeachMob(Action<MobEntity> action);

    /// <summary>rAthena <c>map_foreachinmap</c>.</summary>
    int ForeachInMap(int mapId, Action<Entity> action);

    /// <summary>rAthena <c>map_foreachinrange</c>.</summary>
    int ForeachInRange(int mapId, short cx, short cy, short range, Action<Entity> action);

    /// <summary>rAthena <c>map_getcell</c>.</summary>
    bool GetCell(int mapId, short x, short y, int cellChk);

    /// <summary>rAthena <c>map_setcell</c>.</summary>
    void SetCell(int mapId, short x, short y, int cellChk, bool set);

    /// <summary>rAthena <c>map_getmapflag</c>.</summary>
    long GetMapFlag(int mapId, int flag);

    /// <summary>rAthena <c>map_setmapflag</c>.</summary>
    void SetMapFlag(int mapId, int flag, long value);

    /// <summary>rAthena <c>map_addnpc</c>.</summary>
    int AddNpc(int mapId, NpcEntity npc);

    /// <summary>rAthena <c>do_init_map</c> / <c>do_final_map</c>.</summary>
    void Init();
    void Final();

    /// <summary>rAthena <c>map_reload</c>.</summary>
    void Reload();
}
