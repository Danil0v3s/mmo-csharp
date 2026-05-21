using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ELECTRICWALK — Sorcerer Electric Walk. Manual port of
/// <c>rathena-fork/src/map/skills/mage/electricwalk.cpp</c>.
///
/// <para>Self-buff that paints electrified ground while the caster
/// moves. Ratio: <c>+(-100 + 60*lv)</c>; SC_BLAST_OPTION job-level/2
/// bonus is TODO. Buff replaces any active SC_ELECTRICWALK on the
/// caster before starting fresh.</para>
/// </summary>
public sealed class ElectricWalk : SkillImpl
{
    public ElectricWalk() : base(SkillIds.SO_ELECTRICWALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 60*lv; SC_BLAST_OPTION job_level/2 TODO.
        return baseRatio + (-100 + 60 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena ends prior SC_ELECTRICWALK, then restarts with val1=skillId, val2=lv, duration=time.
        // SC_ELECTRICWALK is not yet on our StatusType enum — buff TODO.
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
