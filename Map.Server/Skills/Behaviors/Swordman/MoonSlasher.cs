using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_MOONSLASHER — Royal Guard Moon Slasher. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/moonslasher.cpp</c>.
/// Ratio <c>+(-100 + 120*lv) + 80*OverBrand_lv</c>. On hit applies
/// SC_OVERBRANDREADY to the caster.
/// </summary>
public sealed class MoonSlasher : RecursiveDamageSplashSkillImpl
{
    public MoonSlasher() : base(SkillIds.LG_MOONSLASHER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var overBrandLv = (src as PlayerEntity)?.LearnedSkills.GetValueOrDefault(SkillIds.LG_OVERBRAND) ?? 0;
        return baseRatio + (-100 + 120 * skillLevel) + 80 * overBrandLv;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(src, StatusType.Overbrandready, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
}
