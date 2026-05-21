using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_DANCING_KNIFE — Dancing Knife. Manual port of
/// <c>rathena-fork/src/map/skills/thief/dancingknife.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 200*lv) + 5*pow</c>. SC start on
/// target is applied via the additional-effect chain.
/// </summary>
public sealed class DancingKnife : RecursiveDamageSplashSkillImpl
{
    public DancingKnife() : base(SkillIds.SHC_DANCING_KNIFE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel) + 5 * src.Stats.Pow;
}
