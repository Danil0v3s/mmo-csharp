using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HUUMARANKA — Swirling Petal. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/swirlingpetal.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 150*lv) + STR + NJ_HUUMA*100</c>
/// with SC_KAGEMUSYA val2 multiplier when active. Partner-skill bonus
/// and Kagemusya scaling are TODO.
/// </summary>
public sealed class SwirlingPetal : RecursiveDamageSplashSkillImpl
{
    public SwirlingPetal() : base(SkillIds.KO_HUUMARANKA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 150 * skillLevel) + src.Stats.Str;
}
