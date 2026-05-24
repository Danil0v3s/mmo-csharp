using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_FUUMAKOUCHIKU — Huuma Shuriken Construct
/// (skill.cpp:SS_FUUMAKOUCHIKU arm). Ratio:
/// <c>baseRatio + (-100 + 900 + 1750*lv) + 5*POW +
/// pc_checkskill(SS_FUUMASHOUAKU) * 100 * lv</c>. The
/// <c>SKILL_ALTDMG_FLAG</c> (+200) variant is reserved for the
/// directional-AoE secondary hit — single-target damage stays on the
/// base path.
/// </summary>
public sealed class HuumaShurikenConstruct : WeaponSkillImpl
{
    public HuumaShurikenConstruct() : base(SkillIds.SS_FUUMAKOUCHIKU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 900 + 1750 * skillLevel);
        ratio += 5 * src.Stats.Pow;
        if (src is PlayerEntity pc)
        {
            var fuumaLv = pc.LearnedSkills.GetValueOrDefault(SkillIds.SS_FUUMASHOUAKU);
            if (fuumaLv > 0) ratio += fuumaLv * 100 * skillLevel;
        }
        return ratio;
    }
}
