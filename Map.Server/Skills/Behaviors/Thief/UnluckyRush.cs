using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_UNLUCKY_RUSH — Unlucky Rush. Manual port of
/// <c>rathena-fork/src/map/skills/thief/unluckyrush.cpp</c>.
/// Caster slides to target then attacks. Ratio
/// <c>+(-100 + 100 + 300*lv) + 5*pow</c>; +2500*lv under SC_CHASING.
/// Slide + SC_HANDICAPSTATE_MISFORTUNE follow-up are TODO.
/// </summary>
public sealed class UnluckyRush : WeaponSkillImpl
{
    public UnluckyRush() : base(SkillIds.ABC_UNLUCKY_RUSH) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 100 + 300 * skillLevel) + 5 * src.Stats.Pow;
}
