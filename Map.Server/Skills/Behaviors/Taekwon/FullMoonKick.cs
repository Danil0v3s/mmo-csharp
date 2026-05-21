using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_FULLMOONKICK — Recursive splash; ratio +(1000 + 100*lv). 15 + 5*lv% blind on hit.</summary>
public sealed class FullMoonKick : RecursiveDamageSplashSkillImpl
{
    public FullMoonKick() : base(SkillIds.SJ_FULLMOONKICK) { }
    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 1000 + 100 * skillLevel;
    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 15 + 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
