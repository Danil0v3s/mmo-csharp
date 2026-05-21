using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// ABC_DEFT_STAB — Deft Stab. Manual port of
/// <c>rathena-fork/src/map/skills/thief/deftstab.cpp</c>.
/// Recursive splash; ratio <c>+(-100 + 700 + 550*lv) + 7*pow</c>.
/// </summary>
public sealed class DeftStab : RecursiveDamageSplashSkillImpl
{
    public DeftStab() : base(SkillIds.ABC_DEFT_STAB) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 700 + 550 * skillLevel) + 7 * src.Stats.Pow;
}
