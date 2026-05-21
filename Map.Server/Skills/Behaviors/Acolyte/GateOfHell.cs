using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_GATEOFHELL — Sura Gate of Hell. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/gateofhell.cpp</c>.
/// Ratio <c>+(-100 + 500*lv)</c>; combo path <c>+(-100 + 800*lv)</c>
/// when SC_COMBO is active (SC-aware ratio hook is TODO).
/// </summary>
public sealed class GateOfHell : WeaponSkillImpl
{
    public GateOfHell() : base(SkillIds.SR_GATEOFHELL) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena base ratio: fallen-empire combo path = -100 + 800*lv,
        // otherwise -100 + 500*lv. RE_LVL_DMOD(100) renewal modifier
        // applied by the damage formula at calc time.
        //
        // SC_COMBO / SC_GT_REVITALIZE reader isn't passed through this
        // hook signature yet. Without SC visibility we land the regular
        // (no-combo) path as a faithful baseline; combo bonus is a TODO
        // pending an SC-aware ratio hook.
        return baseRatio + (-100 + 500 * skillLevel);
    }
}
