using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_CROSS_SLASH — Cross Slash. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossslash.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 300*lv) + 5*pow</c>. Under
/// SC_SHADOW_EXCEED the bonus +60*lv +2*pow stacks on top.
/// </summary>
public sealed class CrossSlash : RecursiveDamageSplashSkillImpl
{
    public CrossSlash() : base(SkillIds.SHC_CROSS_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 300 * skillLevel) + 5 * src.Stats.Pow;
        if (ctx.Sc?.Get(src, StatusType.ShadowExceed) != null)
        {
            ratio += 60 * skillLevel;
            ratio += 2 * src.Stats.Pow;
        }
        return ratio;
    }
}
