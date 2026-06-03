using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_BAKURETSU — Kunai Explosion. rAthena <c>battle_calc_attack_skill_ratio</c>
/// KO_BAKURETSU arm (battle.cpp:5663): ratio <c>-100 + NJ_TOBIDOUGU*(50 + dex/4)*lv*4/10</c>,
/// <c>RE_LVL_DMOD(120)</c>, then <c>+10*job_level</c> (post-dmod), then the SC_KAGEMUSYA multiply.
///
/// <para>The real <c>pc_checkskill(NJ_TOBIDOUGU)</c> factor (currently hardcoded 1), the
/// post-dmod <c>10*job_level</c> placement, and the SC_KAGEMUSYA multiply are tracked in
/// <b>COMBAT-91</b>: this splash arm's damage path
/// (<see cref="RecursiveDamageSplashSkillImpl.SplashDamage"/>) is not yet wired to a
/// ratio-scaled value, so none of the ratio refinements (incl. COMBAT-75's KAGEMUSYA close)
/// are reachable here until that lands.</para>
/// </summary>
public sealed class KunaiExplosion : RecursiveDamageSplashSkillImpl
{
    public KunaiExplosion() : base(SkillIds.KO_BAKURETSU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + -100 + 1 * (50 + src.Stats.Dex / 4) * skillLevel * 4 / 10;
        ratio += 10;
        return ratio;
    }
}
