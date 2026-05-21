using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_JUDEX — Arch Bishop Judex. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/judex.cpp</c>.
/// Ratio <c>+(-100 + 300 + 70*lv)</c>.
/// </summary>
public sealed class Judex : RecursiveDamageSplashSkillImpl
{
    public Judex() : base(SkillIds.AB_JUDEX) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: skillratio += -100 + 300 + 70 * skill_lv;  RE_LVL_DMOD(100);
        // Result: (200 + 70*lv) % — lv1 270 %, lv5 550 %, lv10 900 %.
        // RE_LVL_DMOD applied in the renewal damage formula at calc time.
        return baseRatio + (-100 + 300 + 70 * skillLevel);
    }
}
