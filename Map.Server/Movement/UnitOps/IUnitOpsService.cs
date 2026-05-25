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

    /// <summary>
    /// rAthena <c>skill_check_unit_movepos</c> (skill.cpp:9099) —
    /// teleport-near-target helper used by Charge Attack, Shadow Leap,
    /// Final Strike, Cross Impact, Illusion Bewitch, and similar
    /// movement-and-strike skills. Tries to place <paramref name="bl"/>
    /// adjacent to <paramref name="x"/>/<paramref name="y"/> on the
    /// same map; picks the first walkable cell in cardinal order
    /// (N, E, S, W). Returns true on success, false if no walkable
    /// neighbor cell exists (rare; caller should fall back to staying
    /// in place).
    /// </summary>
    bool CheckUnitMovePos(Entity bl, short x, short y, byte easy);

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

    // ---- Wave 73: small canonical helpers from unit.cpp ----

    /// <summary>
    /// rAthena <c>unit_is_walking</c> (unit.cpp:1402). True iff the
    /// entity currently has an active <see cref="WalkState"/>.
    /// </summary>
    bool IsWalking(Entity bl);

    /// <summary>
    /// rAthena <c>unit_can_attack</c> (unit.cpp:2580). True iff
    /// <paramref name="bl"/> could legitimately swing at
    /// <paramref name="target"/> right now (same map, alive, not
    /// frozen, not CC'd). Standalone predicate without the side-effect
    /// of latching an AttackState — useful for AI gates and pre-cast
    /// validation.
    /// </summary>
    bool CanAttack(Entity bl, Entity target);

    /// <summary>
    /// rAthena <c>unit_set_target</c> (unit.cpp:2486) — swap the
    /// attacker's target without restarting the attack timer.
    /// Equivalent to <see cref="Attack"/> when there's no prior latch;
    /// when there is, only the target id moves (AttackableTick stays).
    /// Maintains the rAthena <c>unit_counttargeted</c> book-keeping.
    /// </summary>
    bool SetTarget(Entity src, EntityId newTargetId);

    /// <summary>
    /// rAthena <c>unit_changetarget</c> (unit.cpp:2520) — alias for
    /// <see cref="SetTarget"/> that explicitly broadcasts the intent
    /// (rAthena scans every same-map entity attacking the old target
    /// and re-points it; our model keeps the counter on the target so
    /// the loop collapses to a single dec/inc).
    /// </summary>
    bool ChangeTarget(Entity src, EntityId newTargetId);

    /// <summary>
    /// rAthena <c>unit_unattackable</c> (unit.cpp:2562). Drops the
    /// caster's attack lock and stops their auto-attack timer. The
    /// brief post-cast immunity window (rAthena pc_unsetshield_*) is a
    /// caller concern; this helper only handles the lock release.
    /// </summary>
    void Unattackable(Entity bl);

    /// <summary>
    /// rAthena <c>unit_skillcastcancel</c> (unit.cpp:2380) — abort
    /// any in-flight skill cast. Returns true iff a cast was actually
    /// cancelled.
    /// </summary>
    bool SkillCastCancel(Entity bl);

    /// <summary>
    /// rAthena <c>unit_refresh</c> (unit.cpp:3148) — re-broadcast the
    /// entity's wire state to its AOI (used after gear / view / size
    /// changes so the surrounding clients re-render). C# parity:
    /// vanish + respawn on the same cell, which retriggers the
    /// <c>ZC_NOTIFY_STANDENTRY</c> pipeline.
    /// </summary>
    void Refresh(Entity bl);

    /// <summary>
    /// rAthena <c>unit_addshadowscar</c> (unit.cpp:4258) — append a
    /// Shadow Scar timer entry to <paramref name="bl"/>. Caps at
    /// <see cref="Entities.Entity.MaxShadowScar"/>; on cap the call is
    /// a logged no-op. Each entry expires after
    /// <paramref name="intervalMs"/> milliseconds; the
    /// <see cref="Status.IStatusChangeService.Tick"/> pump prunes
    /// expired entries. After modifying the list the helper writes
    /// the new count into SC_SHADOW_SCAR's Val1 so combat consumers
    /// read the live scar count.
    /// </summary>
    void AddShadowScar(Entity bl, int intervalMs);
}
