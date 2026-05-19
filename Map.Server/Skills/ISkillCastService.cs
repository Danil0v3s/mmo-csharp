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
    /// Resolve a skill RIGHT NOW (no cast timer). Used by mob skills,
    /// auto-cast procs, and tests. Returns true if anything happened.
    /// </summary>
    bool ResolveSkill(Entity source, Entity target, ushort skillId, ushort skillLevel);

    /// <summary>Tick — advance pending cast timers and resolve casts whose timer elapsed.</summary>
    void Tick(long nowTick);
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
}
