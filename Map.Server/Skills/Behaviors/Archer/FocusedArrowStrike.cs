using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// SN_SHARPSHOOTING — Sniper Focused Arrow Strike. Manual port of
/// <c>rathena-fork/src/map/skills/archer/focusedarrowstrike.cpp</c>.
///
/// <para>Renewal ratio: <c>+(-100 + 300 + 300*lv)</c>. Mob casters
/// retain the pre-renewal formula (<c>+(100 + 50*lv)</c>; splash hits
/// add <c>+(-100 + 140*lv)</c> when the secondary-hit miscflag bit 2
/// is set). Ends SC_CAMOUFLAGE on splash hits.</para>
/// </summary>
public sealed class FocusedArrowStrike : RecursiveDamageSplashSkillImpl
{
    public FocusedArrowStrike() : base(SkillIds.SN_SHARPSHOOTING) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        if (src is MobEntity)
        {
            if ((miscflag & 2) != 0)
                return baseRatio + (-100 + 140 * skillLevel);
            return baseRatio + 100 + 50 * skillLevel;
        }
        return baseRatio + (-100 + 300 + 300 * skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(src, StatusType.Camouflage);
    }
}
