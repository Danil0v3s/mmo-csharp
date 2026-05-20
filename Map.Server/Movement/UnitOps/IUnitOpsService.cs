using Map.Server.Entities;

namespace Map.Server.Movement.UnitOps;

/// <summary>
/// Entity-action helpers — the rAthena <c>unit.cpp</c> public
/// surface (4 010 lines, 51 functions). MovementService already
/// covers walking + path scheduling; this service surfaces the
/// rAthena-named operations (warp, stop_walking, stop_attack,
/// blown_by, set_dir, attack, can_move, …).
/// </summary>
public interface IUnitOpsService
{
    /// <summary>rAthena <c>unit_warp</c> — same-map / cross-map teleport.</summary>
    int Warp(Entity bl, string mapName, short x, short y, byte flag);

    /// <summary>rAthena <c>unit_walktoxy</c>.</summary>
    bool WalkToXy(Entity bl, short x, short y, byte flag);

    /// <summary>rAthena <c>unit_walktobl</c>.</summary>
    bool WalkToBl(Entity bl, Entity target, short range, byte flag);

    /// <summary>rAthena <c>unit_stop_walking</c>.</summary>
    bool StopWalking(Entity bl, byte type);

    /// <summary>rAthena <c>unit_stop_attack</c>.</summary>
    bool StopAttack(Entity bl);

    /// <summary>rAthena <c>unit_can_move</c>.</summary>
    bool CanMove(Entity bl);

    /// <summary>rAthena <c>unit_attack</c>.</summary>
    bool Attack(Entity src, EntityId targetId, byte continuous);

    /// <summary>rAthena <c>unit_blown_by</c>.</summary>
    bool BlownBy(Entity bl, int direction, int count);

    /// <summary>rAthena <c>unit_setdir</c>.</summary>
    bool SetDir(Entity bl, byte dir);

    /// <summary>rAthena <c>unit_getdir</c>.</summary>
    byte GetDir(Entity bl);

    /// <summary>rAthena <c>unit_movepos</c>.</summary>
    bool MovePos(Entity bl, short x, short y, byte easy, bool checkColl);

    /// <summary>rAthena <c>unit_skilluse_id</c>.</summary>
    bool SkillUseId(Entity src, EntityId targetId, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>unit_skilluse_pos</c>.</summary>
    bool SkillUsePos(Entity src, short x, short y, ushort skillId, ushort skillLevel);

    /// <summary>rAthena <c>unit_remove_map</c>.</summary>
    int RemoveMap(Entity bl, byte clearType);

    /// <summary>rAthena <c>unit_free</c>.</summary>
    int Free(Entity bl, byte clearType);

    /// <summary>rAthena <c>unit_changeviewsize</c>.</summary>
    int ChangeViewSize(Entity bl, short size);

    /// <summary>rAthena <c>unit_data_create</c>.</summary>
    void DataCreate(Entity bl);

    /// <summary>rAthena <c>unit_can_reach_bl</c>.</summary>
    bool CanReachBl(Entity src, Entity target, short range);

    /// <summary>rAthena <c>unit_can_reach_pos</c>.</summary>
    bool CanReachPos(Entity src, short x, short y, byte easy);
}
