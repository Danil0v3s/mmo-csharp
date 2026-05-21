using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_SECOND_FAITH — Inquisitor Second Faith. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/secondfaith.cpp</c>.
///
/// <para>Smallest of the Second-* brand trio. Ratio:
/// <c>-100 + 100 + 2300*lv + 5*POW</c>. Brands target with
/// <see cref="StatusType.SecondBrand"/>.</para>
/// </summary>
public sealed class SecondFaith : RecursiveDamageSplashSkillImpl
{
    public SecondFaith() : base(SkillIds.IQ_SECOND_FAITH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 100 + 2300 * skillLevel) + 5 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.SecondBrand,
            val1: skillLevel, 0, 0, 0, durationMs: 4000, src);
    }
}
