using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_JYUMONJIKIRI — Cross Slash. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/kocrossslash.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c> with the RE_LVL_DMOD(120) base-level scalar
/// (battle.cpp:5641). The <c>+lv*srcLv</c> SC_JYUMONJIKIRI ratio bonus and the
/// position-shift + double-hit behavior are pending (→ COMBAT-57).
/// </summary>
public sealed class KoCrossSlash : WeaponSkillImpl
{
    public KoCrossSlash() : base(SkillIds.KO_JYUMONJIKIRI) { }

    // COMBAT-35 — RE_LVL_DMOD(120) (battle.cpp:5641). ComputeSkillDamage applies
    // this divisor to the ratio above base level 99.
    protected override int ReLvlDivisor => 120;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Jyumonjikiri, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
}
