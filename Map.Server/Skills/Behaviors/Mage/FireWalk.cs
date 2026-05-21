using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_FIREWALK — Sorcerer Fire Walk. Manual port of
/// <c>rathena-fork/src/map/skills/mage/firewalk.cpp</c>.
///
/// <para>Self-buff that paints burning ground while the caster moves.
/// Ratio: <c>+(-100 + 60*lv)</c>; SC_HEATER_OPTION job-level/2 bonus is
/// TODO. Buff replaces any active SC_FIREWALK before starting fresh —
/// SC_FIREWALK is not yet on our StatusType enum (TODO).</para>
/// </summary>
public sealed class FireWalk : SkillImpl
{
    public FireWalk() : base(SkillIds.SO_FIREWALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 60*lv; SC_HEATER_OPTION job_level/2 TODO.
        return baseRatio + (-100 + 60 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena ends prior SC_FIREWALK then restarts. SC_FIREWALK not on enum — buff TODO.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
