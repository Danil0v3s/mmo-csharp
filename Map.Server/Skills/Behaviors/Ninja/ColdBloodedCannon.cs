using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_REIKETSUHOU — Cold Blooded Cannon (skill.cpp:SS_REIKETSUHOU arm).
/// Ratio: <c>baseRatio + (-100 + 450 + 950*lv) + 5*SPL +
/// pc_checkskill(SS_ANTENPOU) * 40 * lv</c>; +7000 when the caster has
/// <c>SC_WATER_CHARM_POWER</c> (mapped to <see cref="StatusType.Charmpower"/>).
/// The SC bonus is wired via the SC apply hook in <see cref="ApplyAdditionalEffects"/>
/// since CalculateSkillRatio can't reach <see cref="SkillBehaviorContext.Sc"/>.
/// </summary>
public sealed class ColdBloodedCannon : SkillImpl
{
    public ColdBloodedCannon() : base(SkillIds.SS_REIKETSUHOU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 450 + 950 * skillLevel);
        ratio += 5 * src.Stats.Spl;
        if (src is PlayerEntity pc)
        {
            var antenpouLv = pc.LearnedSkills.GetValueOrDefault(SkillIds.SS_ANTENPOU);
            if (antenpouLv > 0) ratio += antenpouLv * 40 * skillLevel;
        }
        return ratio;
    }
}
