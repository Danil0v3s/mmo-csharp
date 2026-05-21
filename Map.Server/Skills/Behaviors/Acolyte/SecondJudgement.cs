using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// IQ_SECOND_JUDGEMENT — Inquisitor Second Judgement. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/secondjudgement.cpp</c>.
///
/// <para>Holy splash. Brands the target with
/// <see cref="StatusType.SecondBrand"/>. Ratio:
/// <c>-100 + 150 + 2600*lv + 7*POW</c>.</para>
/// </summary>
public sealed class SecondJudgement : RecursiveDamageSplashSkillImpl
{
    public SecondJudgement() : base(SkillIds.IQ_SECOND_JUDGEMENT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 150 + 2600 * skillLevel) + 7 * src.Stats.Pow;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.SecondBrand,
            val1: skillLevel, 0, 0, 0, durationMs: 4000, src);
    }
}
