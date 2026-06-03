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
    /// COMBAT-87 — <see cref="CanAct"/> plus the <c>NoAttack</c> caster state: a few SCs let the
    /// entity still cast (to re-toggle the SC) but forbid auto-attacking. Renewal SC_BASILICA
    /// (db/re/status.yml <c>States: NoAttack</c>) is the one we model — the Basilica caster cannot
    /// auto-attack while it is up, but can re-cast HP_BASILICA to cancel it. Consulted by the
    /// auto-attack path; the skill path keeps using <see cref="CanCastSkill"/>/<see cref="CanAct"/>.
    /// </summary>
    public static bool CanAttack(this Entity entity, IStatusChangeService? sc)
    {
        if (!entity.CanAct(sc)) return false;
        if (sc != null && sc.Get(entity, StatusType.Basilica) != null) return false;
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

    /// <summary>
    /// rAthena <c>status_check_visibility</c> (status.cpp:2292). True iff
    /// <paramref name="src"/> can actually see <paramref name="target"/>
    /// right now. Used by AI engines, AOI broadcast filters, and skill
    /// targeting validation. Reads the target's hiding-class SCs and
    /// honors the source's <c>MD_DETECTOR</c> mob mode + boss-class
    /// override.
    ///
    /// <para>The full hiding set (rAthena status.cpp:2323-2343):
    /// SC_HIDING, SC_CLOAKING, SC_CHASEWALK, SC_CAMOUFLAGE, SC_STEALTHFIELD,
    /// SC_SUHIDE, SC_CLOAKINGEXCEED, SC_NEWMOON, SC__FEINTBOMB,
    /// SC_ELEMENTAL_VEIL. Boss-class sources see through everything;
    /// MD_DETECTOR sees through the base hide set but not the
    /// "perfect hiding" overlay (SC_CLOAKINGEXCEED, SC_NEWMOON).</para>
    ///
    /// <para>The <paramref name="checkBlind"/> flag clamps the source's
    /// view range to 1 when SC_BLIND is on the source. We don't apply the
    /// range portion here (callers own distance gating); only the SC
    /// predicate fires.</para>
    /// </summary>
    public static bool CanSee(this Entity src, Entity target, IStatusChangeService? sc)
    {
        if (sc == null) return true;
        // Source-side detector bits: boss class + MD_DETECTOR override the
        // base hide set; perfect-hiding overlay still excludes detectors.
        var isBoss = (src.Stats.Mode & MobMode.Mvp) != 0;
        // COMBAT-65 — a PC with the Intravision equip flag (bIntravision /
        // special_state.intravision, rAthena status.cpp:3756) sees through the base
        // hide set like a detector (Hiding/Cloaking/Camouflage/…), but not perfect-hide.
        var isDetector = (src.Stats.Mode & MobMode.Detector) != 0
            || (src is Map.Server.Entities.PlayerEntity ipc && ipc.EquipBonuses.Intravision);

        // Base hide set — Hiding / Cloaking / Camouflage / Stealthfield /
        // Suhide / Chasewalk2 all gate "low-tier" detection.
        var baseHide =
            sc.Get(target, StatusType.Hiding) != null ||
            sc.Get(target, StatusType.Cloaking) != null ||
            sc.Get(target, StatusType.Chasewalk2) != null ||
            sc.Get(target, StatusType.Camouflage) != null ||
            sc.Get(target, StatusType.Stealthfield) != null ||
            sc.Get(target, StatusType.Suhide) != null;
        if (baseHide && !isBoss && !isDetector) return false;

        // Perfect-hide overlay — SC_CLOAKINGEXCEED / SC_NEWMOON. Bosses
        // and detectors both pierce baseHide but NOT this overlay
        // (rAthena: detector still blocked unless perfect_hiding bonus).
        var perfectHide =
            sc.Get(target, StatusType.Cloakingexceed) != null ||
            sc.Get(target, StatusType.Newmoon) != null;
        if (perfectHide && !isBoss) return false;

        // Feint Bomb — SC__FEINTBOMB. Always hides unless detector.
        if (sc.Get(target, StatusType.Feintbomb) != null && !isBoss && !isDetector) return false;

        // Elemental Veil — SC_ELEMENTAL_VEIL. Hides elementals from
        // non-detector / non-boss observers.
        if (sc.Get(target, StatusType.ElementalVeil) != null && !isBoss && !isDetector) return false;

        return true;
    }
}
