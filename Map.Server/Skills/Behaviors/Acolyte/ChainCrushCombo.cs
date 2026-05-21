using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// CH_CHAINCRUSH — Champion Chain Crush Combo. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/chaincrushcombo.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>; GT_ENERGYGAIN multiplier is TODO.
/// </summary>
public sealed class ChainCrushCombo : WeaponSkillImpl
{
    public ChainCrushCombo() : base(SkillIds.CH_CHAINCRUSH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // Renewal: skillratio += -100 + 200 * skill_lv;  RE_LVL_DMOD(100);
        // Final: (-100 + 200*lv) + base 100 = 200*lv % at base level.
        var ratio = baseRatio + (-100 + 200 * skillLevel);

        // GT_ENERGYGAIN buff bumps skillratio by +50 % multiplicatively.
        // SC reader isn't available in CalculateSkillRatio's hook signature —
        // TODO: thread ISC through this hook so the multiplier lands.
        return ratio;
    }
}
