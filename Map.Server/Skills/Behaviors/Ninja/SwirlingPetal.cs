using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_HUUMARANKA — Swirling Petal. rAthena <c>battle_calc_attack_skill_ratio</c>
/// KO_HUUMARANKA arm (battle.cpp:5647): ratio <c>-100 + 150*lv + STR + NJ_HUUMA*100</c>,
/// <c>RE_LVL_DMOD(100)</c>, then the SC_KAGEMUSYA caster multiply.
///
/// <para>The <c>NJ_HUUMA*100</c> partner-skill term and the SC_KAGEMUSYA multiply are
/// tracked in <b>COMBAT-91</b>: this splash arm's damage path
/// (<see cref="RecursiveDamageSplashSkillImpl.SplashDamage"/>) is not yet wired to a
/// ratio-scaled value, so neither the base ratio nor the KAGEMUSYA close (COMBAT-75's
/// <c>CalculateSkillRatioPostDmodMultiply</c>) is reachable here until that lands.</para>
/// </summary>
public sealed class SwirlingPetal : RecursiveDamageSplashSkillImpl
{
    public SwirlingPetal() : base(SkillIds.KO_HUUMARANKA) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 150 * skillLevel) + src.Stats.Str;
}
