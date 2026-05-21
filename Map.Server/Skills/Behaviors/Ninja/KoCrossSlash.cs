using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_JYUMONJIKIRI — Cross Slash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kocrossslash.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>; +lv*srcLv when target already has
/// SC_JYUMONJIKIRI. Position-shift + double-hit logic is TODO.
/// </summary>
public sealed class KoCrossSlash : WeaponSkillImpl
{
    public KoCrossSlash() : base(SkillIds.KO_JYUMONJIKIRI) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Jyumonjikiri, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
}
