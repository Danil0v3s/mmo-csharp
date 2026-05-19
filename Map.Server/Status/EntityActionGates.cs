using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// Centralized "can this entity act right now?" gate. Mirrors rAthena
/// <c>pc_cant_act</c> (pc.hpp) and <c>status_check_skilluse</c>
/// (status.cpp:1763). Both treat the OPT1 group — STONE / FREEZE / STUN
/// / SLEEP — as hard blocks: attack, skill use, item use, drop, sit/stand
/// all return early. Confusion (OPT2) blocks skill targeting but not
/// melee, so it's not folded into the global gate.
///
/// As more SCs port (M-M1 in [parity-audit-2026-05-19.md]), this is the
/// single place that grows; per-handler ad-hoc checks would drift apart.
/// </summary>
public static class EntityActionGates
{
    /// <summary>
    /// True if the entity is free to take a player-initiated action.
    /// rAthena pc_cant_act = OPT1 active (STONE/FREEZE/STUN/SLEEP) ||
    /// dead. The dead-check stays in <c>IPcDeathService</c>; this method
    /// covers the SC slice and is composable with the existing IsDead
    /// gate at each call site.
    /// </summary>
    public static bool CanAct(this Entity entity, IStatusChangeService? sc)
    {
        if (sc == null) return true;
        // OPT1 group — see rAthena status.cpp:status_change_start cases
        // for SC_STONE/SC_FREEZE/SC_STUN/SC_SLEEP (~status.cpp:11050+).
        // Any one of them sets opt1 and pc_cant_act returns true.
        if (sc.Get(entity, StatusType.Stone) != null) return false;
        if (sc.Get(entity, StatusType.Freeze) != null) return false;
        if (sc.Get(entity, StatusType.Stun) != null) return false;
        if (sc.Get(entity, StatusType.Sleep) != null) return false;
        return true;
    }

    /// <summary>
    /// Subset of <see cref="CanAct"/> tailored to skill use. rAthena
    /// <c>status_check_skilluse</c> adds Silence and Confusion to the
    /// OPT1 set — Silence blocks magic, Confusion blocks targeting.
    /// We treat them uniformly here; per-skill type filtering can move
    /// into the resolver if/when we differentiate physical vs magic.
    /// </summary>
    public static bool CanCastSkill(this Entity entity, IStatusChangeService? sc)
    {
        if (!entity.CanAct(sc)) return false;
        if (sc == null) return true;
        if (sc.Get(entity, StatusType.Silence) != null) return false;
        if (sc.Get(entity, StatusType.Confusion) != null) return false;
        return true;
    }
}
