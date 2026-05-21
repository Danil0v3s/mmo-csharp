using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_RAID — Sightless Mind / Raid. Manual port of
/// <c>rathena-fork/src/map/skills/thief/sightlessmind.cpp</c>.
/// Recursive splash; renewal ratio <c>+(-100 + 50 + 150*lv)</c>.
/// Applies Stun + Blind at <c>10 + 3*lv</c>% each. Ends SC_HIDING on
/// cast.
/// </summary>
public sealed class SightlessMind : RecursiveDamageSplashSkillImpl
{
    public SightlessMind() : base(SkillIds.RG_RAID) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 50 + skillLevel * 150);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 10 + 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
        if (System.Random.Shared.Next(100) < 10 + 3 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Blind, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
        ctx.Sc?.End(src, StatusType.Hiding);
    }
}
