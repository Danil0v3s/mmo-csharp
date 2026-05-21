using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_FATAL_SHADOW_CROW — Fatal Shadow Crow. Manual port of
/// <c>rathena-fork/src/map/skills/thief/fatalshadowcrow.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 1300*lv + 10*pow)</c>;
/// +150*lv vs Demihuman / Dragon. Applies SC_DARKCROW. Slide-behind
/// is TODO.
/// </summary>
public sealed class FatalShadowCrow : RecursiveDamageSplashSkillImpl
{
    public FatalShadowCrow() : base(SkillIds.SHC_FATAL_SHADOW_CROW) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 1300 * skillLevel) + 10 * src.Stats.Pow;
        if (target.Stats.Race == BattleRace.Demihuman || target.Stats.Race == BattleRace.Dragon)
            ratio += 150 * skillLevel;
        return ratio;
    }
}
