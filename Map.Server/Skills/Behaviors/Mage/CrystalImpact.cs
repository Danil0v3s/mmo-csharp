using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_CRYSTAL_IMPACT — Arch Mage Crystal Impact. Manual port of
/// <c>rathena-fork/src/map/skills/mage/crystalimpact.cpp</c>.
///
/// <para>AOE wind magic centered on the target with aftershock chain hit.
/// Ratio: <c>+(-100 + 250 + 1300*lv) + 5*SPL</c>. INFRA-DEFERRED: SC_CLIMAX
/// modes (lv 1 turns the splash into an ally buff, lv 2 doubles the hit
/// count, lv 5 enlarges the AOE to 15×15) need <c>ctx</c> exposure on
/// <see cref="ModifyDamageData"/> and on the splash-radius / cast-arm
/// branch hooks — none of those hooks receive <c>SkillBehaviorContext</c>
/// today.</para>
/// </summary>
public sealed class CrystalImpact : RecursiveDamageSplashSkillImpl
{
    public CrystalImpact() : base(SkillIds.AG_CRYSTAL_IMPACT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 250 + 1300*lv + 5*SPL.
        // The Climax buff additive is applied through pc_skillatk_bonus, not here.
        return baseRatio + (-100 + 250 + 1300 * skillLevel) + 5 * src.Stats.Spl;
    }
}
