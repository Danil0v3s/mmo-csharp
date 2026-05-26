using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_ARROWSTORM — Ranger Arrow Storm. Manual port of
/// <c>rathena-fork/src/map/skills/archer/arrowstorm.cpp</c>.
///
/// <para>Ratio: <c>+(-100 + 200 + 180*lv)</c> baseline; with
/// SC_FEARBREEZE on the caster, <c>+(-100 + 200 + 250*lv)</c>.
/// SC_CAMOUFLAGE on the caster is ended after the splash search
/// (rAthena <c>splashSearch</c> override).</para>
/// </summary>
public sealed class ArrowStorm : RecursiveDamageSplashSkillImpl
{
    public ArrowStorm() : base(SkillIds.RA_ARROWSTORM) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        if (ctx.Sc != null && ctx.Sc.Get(src, StatusType.Fearbreeze) != null)
            return baseRatio + (-100 + 200 + 250 * skillLevel);
        return baseRatio + (-100 + 200 + 180 * skillLevel);
    }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        ctx.Sc?.End(src, StatusType.Camouflage);
    }
}
