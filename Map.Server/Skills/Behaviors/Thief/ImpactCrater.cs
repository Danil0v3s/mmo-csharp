using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SHC_IMPACT_CRATER — Impact Crater. Manual port of
/// <c>rathena-fork/src/map/skills/thief/impactcrater.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 80*lv) + 5*pow</c>.
///
/// <para>🚩 INFRA-DEFERRED — rAthena reads
/// <c>sc->getSCE(SC_ROLLINGCUTTER)->val1</c> inside
/// <c>modifyDamageData</c> and sets <c>dmg.div_ = val1</c>. Our
/// <see cref="SkillImpl.ModifyDamageData"/> hook has no
/// <see cref="SkillBehaviorContext"/> param so the SC read needs the
/// signature widened. The ratio / splash / SC_IMPACT_CRATER apply
/// below are on parity — only the per-hit count read is deferred.</para>
/// </summary>
public sealed class ImpactCrater : RecursiveDamageSplashSkillImpl
{
    public ImpactCrater() : base(SkillIds.SHC_IMPACT_CRATER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 80 * skillLevel) + 5 * src.Stats.Pow;
}
