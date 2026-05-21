using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Gunslinger;

/// <summary>
/// RL_SLUGSHOT — Rebellion Slug Shot. Manual port of
/// <c>rathena-fork/src/map/skills/gunslinger/slugshot.cpp</c>.
/// Ratio <c>+(-100 + 1200*lv)</c> vs mobs, <c>+(-100 + 2000*lv)</c> vs
/// players; multiplied by (2 + size). 100% stun on hit. Size mult uses
/// 2 as a default.
/// </summary>
public sealed class SlugShot : WeaponSkillImpl
{
    public SlugShot() : base(SkillIds.RL_SLUGSHOT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = target is PlayerEntity
            ? baseRatio + (-100 + 2000 * skillLevel)
            : baseRatio + (-100 + 1200 * skillLevel);
        return ratio * 2;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
}
