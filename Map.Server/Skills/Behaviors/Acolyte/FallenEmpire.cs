using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_FALLENEMPIRE — Sura Fallen Empire. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/fallenempire.cpp</c>.
/// Ratio <c>+300*lv</c>, RE_LVL_DMOD(150).
/// </summary>
public sealed class FallenEmpire : WeaponSkillImpl
{
    public FallenEmpire() : base(SkillIds.SR_FALLENEMPIRE) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += 300 * skill_lv;  RE_LVL_DMOD(150);
        // Final ATK ratio: (100 + 300*lv) * casterBaseLv / 150
        // RE_LVL_DMOD applied by the renewal damage formula at calc time.
        return baseRatio + 300 * skillLevel;
    }
}
