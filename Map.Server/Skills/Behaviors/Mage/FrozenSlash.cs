using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_FROZEN_SLASH — Arch Mage Frozen Slash. Manual port of
/// <c>rathena-fork/src/map/skills/mage/frozenslash.cpp</c>.
///
/// <para>Water AOE splash. Ratio: <c>+(-100 + 450 + 950*lv) + 5*SPL</c>,
/// with +<c>150 + 350*lv</c> when SC_CLIMAX is active on the caster.
/// RecursiveDamageSplashSkillImpl handles the splash chain.</para>
/// </summary>
public sealed class FrozenSlash : RecursiveDamageSplashSkillImpl
{
    public FrozenSlash() : base(SkillIds.AG_FROZEN_SLASH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 450 + 950 * skillLevel) + 5 * src.Stats.Spl;
        if (ctx.Sc?.Get(src, StatusType.Climax) != null)
            ratio += 150 + 350 * skillLevel;
        return ratio;
    }
}
