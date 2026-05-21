using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// AG_CRYSTAL_IMPACT — Arch Mage Crystal Impact. Manual port of
/// <c>rathena-fork/src/map/skills/mage/crystalimpact.cpp</c>.
///
/// <para>AOE wind magic centered on the target with aftershock chain hit.
/// Ratio: <c>+(-100 + 250 + 1300*lv) + 5*SPL</c>. SC_CLIMAX modes
/// (lv 1 turns the splash into an ally buff, lv 2 doubles the hit
/// count, lv 5 enlarges the AOE to 15×15) are TODO — caster SC
/// readback isn't wired in these hooks.</para>
/// </summary>
public sealed class CrystalImpact : RecursiveDamageSplashSkillImpl
{
    public CrystalImpact() : base(SkillIds.AG_CRYSTAL_IMPACT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 250 + 1300*lv + 5*SPL.
        return baseRatio + (-100 + 250 + 1300 * skillLevel) + 5 * src.Stats.Spl;
    }

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: dmg.div_ = 2 when SC_CLIMAX is active with val1 == 2.
        // SC readback on src TODO — no buff lookup here yet.
    }
}
