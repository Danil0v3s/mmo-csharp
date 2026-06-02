using Map.Server.Entities;

namespace Map.Server.Skills;

/// <summary>
/// Skill use entry point. Port of rAthena's
/// <c>skill_use_id</c> / <c>skill_castend_damage_id</c> /
/// <c>skill_castend_nodamage_id</c> (skill.cpp). First slice:
/// instant-cast resolution; cast-time scheduling lives in
/// <see cref="StartCast"/> and runs on the game tick.
/// </summary>
public interface ISkillCastService
{
    /// <summary>
    /// Validate + begin casting <paramref name="skillId"/> from
    /// <paramref name="source"/> at <paramref name="targetId"/>.
    /// Returns the rejection reason (or <see cref="SkillCastResult.Started"/>).
    /// </summary>
    SkillCastResult StartCast(Entity source, EntityId targetId, ushort skillId, ushort skillLevel);

    /// <summary>
    /// Ground-targeted variant of <see cref="StartCast"/>. Mirrors
    /// rAthena <c>unit_skilluse_pos2</c> (unit.cpp). Default
    /// implementation delegates to <see cref="StartCast"/> against the
    /// caster itself (acceptable parity fallback — the picker still
    /// emits the right call shape, downstream cast resolution sees a
    /// CastendPos2 hook on the SkillImpl).
    /// </summary>
    /// <param name="x">Cast cell X.</param>
    /// <param name="y">Cast cell Y.</param>
    SkillCastResult StartCastAt(Entity source, short x, short y, ushort skillId, ushort skillLevel)
        => StartCast(source, source.Id, skillId, skillLevel);

    /// <summary>
    /// Resolve a skill RIGHT NOW (no cast timer). Used by mob skills,
    /// auto-cast procs, and tests. Returns true if anything happened.
    /// </summary>
    bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel);

    /// <summary>
    /// Resolve a ground-targeted skill RIGHT NOW (no cast timer). Drives
    /// the CastendPos2 plugin hook directly. Used by sub-skill ground
    /// dispatch (AG_VIOLENT_QUAKE → AG_VIOLENT_QUAKE_ATK staggered
    /// spawns, AG_ALL_BLOOM → AG_ALL_BLOOM_ATK), abracadabra picks
    /// (SA_ABRACADABRA → CAST_GROUND skill), and skill_mirage_cast
    /// (SS_ANTENPOU → SS_SHINKIROU mirror). Default no-op so legacy
    /// test stubs that pre-date the cell-dispatch path keep compiling.
    /// </summary>
    bool ResolveSkillAt(Entity source, short x, short y, ushort skillId, ushort skillLevel) => false;

    /// <summary>Tick — advance pending cast timers and resolve casts whose timer elapsed.</summary>
    void Tick(long nowTick);

    /// <summary>
    /// rAthena <c>unit_skillcastcancel</c> (unit.cpp) — abort any in-flight
    /// cast belonging to <paramref name="entityId"/>. Returns true if a
    /// pending cast was found and dropped. SP / cooldown remain mutated
    /// (rAthena does not refund either) — same shape as the C++ helper.
    /// Used by SA_SPELLBREAKER, AB_PRAEFATIO's interrupts, and the
    /// generic stun / freeze cast-break path.
    /// </summary>
    bool CancelCast(EntityId entityId);

    /// <summary>
    /// True when <paramref name="entityId"/> currently has a cast pending
    /// resolution. Used by SA_SPELLBREAKER's success roll to fail-fast
    /// when the target isn't actually casting (rAthena ud-&gt;skilltimer
    /// != INVALID_TIMER check).
    /// </summary>
    bool IsCasting(EntityId entityId);

    /// <summary>
    /// Returns the currently-casting skill id + level for
    /// <paramref name="entityId"/>, or (0, 0) when no cast is pending.
    /// Mirrors rAthena's <c>ud-&gt;skill_id</c> / <c>ud-&gt;skill_lv</c>
    /// reads used by Spell Breaker to drain the cost of the cancelled
    /// spell.
    /// </summary>
    (ushort skillId, ushort skillLevel) GetCurrentCast(EntityId entityId);
}

public enum SkillCastResult
{
    Started,
    TargetUnknown,
    TargetDead,
    OutOfRange,
    NotEnoughSp,
    OnCooldown,
    UnknownSkill,
    LevelOutOfRange,
    InvalidTargetType,
    /// <summary>Map's <c>noskill</c> flag refused the cast (rAthena mapflag).</summary>
    MapRefused,
    /// <summary>
    /// Caster is silenced / stunned / frozen / asleep / confused — rAthena
    /// <c>status_check_skilluse</c> refuses (status.cpp:1763).
    /// </summary>
    CannotAct,

    /// <summary>
    /// COMBAT-58 — an ammo-using skill cast with no/insufficient equipped ammo.
    /// rAthena <c>skill_check_condition_castbegin</c> ammo gate (clif_arrow_fail /
    /// USESKILL_FAIL_NEED_MORE_BULLET).
    /// </summary>
    NeedAmmo,
}
