using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_SECOND_FLAME — Inquisitor Second Flame. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/secondflame.cpp</c>.
///
/// <para>Holy splash variant of the brand combo. Ratio:
/// <c>-100 + 200 + 2900*lv + 9*POW</c>. Brands target with
/// <see cref="StatusType.SecondBrand"/>.</para>
/// </summary>
public sealed class SecondFlame : RecursiveDamageSplashSkillImpl
{
    public SecondFlame() : base(SkillIds.IQ_SECOND_FLAME) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 200 + 2900 * skillLevel) + 9 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.SecondBrand,
            val1: skillLevel, 0, 0, 0, durationMs: 4000, src);
    }
}
