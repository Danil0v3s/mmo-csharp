using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_FUUMAKOUCHIKU — Huuma Shuriken Construct. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/huumashurikenconstruct.cpp</c>.
/// Ratio <c>+(-100 + 900 + 1750*lv) + 5*pow</c> with +200 on alt-flag
/// and +(SS_FUUMASHOUAKU * 100 * lv) per learned partner skill. POS2
/// directional AoE — pos handler is TODO (uses map_foreachindir).
/// </summary>
public sealed class HuumaShurikenConstruct : WeaponSkillImpl
{
    public HuumaShurikenConstruct() : base(SkillIds.SS_FUUMAKOUCHIKU) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var ratio = baseRatio + (-100 + 900 + 1750 * skillLevel);
        ratio += 5 * src.Stats.Pow;
        // Deferred: SKILL_ALTDMG_FLAG (+200) and SS_FUUMASHOUAKU * 100 * lv
        // partner-skill bonus. Both require flow into CalculateSkillRatio that
        // isn't plumbed: the alt-flag comes from the recursive foreachindir
        // splash dispatch, and the partner-skill bonus needs a PlayerSkill
        // lookup which this signature (no SkillBehaviorContext) doesn't expose.
        return ratio;
    }
}
