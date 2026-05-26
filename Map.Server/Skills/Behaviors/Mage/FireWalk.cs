using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_FIREWALK — Sorcerer Fire Walk. Manual port of
/// <c>rathena-fork/src/map/skills/mage/firewalk.cpp</c>.
///
/// <para>Self-buff that paints burning ground while the caster moves.
/// Ratio: <c>+(-100 + 60*lv)</c>; SC_HEATER_OPTION on caster adds
/// <c>job_level/2</c>. The buff is delivered through SC_PROPERTYWALK
/// (rAthena's shared "elemental walk" SC); any active PropertyWalk is
/// ended before restarting so the new element / skill id wins.</para>
/// </summary>
public sealed class FireWalk : SkillImpl
{
    public FireWalk() : base(SkillIds.SO_FIREWALK) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: skillratio += -100 + 60*lv; +job_level/2 when SC_HEATER_OPTION.
        var ratio = baseRatio + (-100 + 60 * skillLevel);
        if (ctx.Sc?.Get(src, StatusType.HeaterOption) != null && src is PlayerEntity pc)
            ratio += pc.JobLevel / 2;
        return ratio;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: end any prior SC_PROPERTYWALK then sc_start2 with val1=skillId, val2=lv.
        ctx.Sc?.End(src, StatusType.Propertywalk);
        ctx.Sc?.Start(src, StatusType.Propertywalk, val1: SkillId, val2: skillLevel, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
    }
}
