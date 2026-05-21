using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// IG_RADIANT_SPEAR — Imperial Guard Radiant Spear. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/radiantspear.cpp</c>.
/// Ratio <c>+(-100 + 3500 + 1150*lv) + 5*POW</c>; +50 per Spear/Sword
/// Mastery level; +250*lv with SC_SPEAR_SCAR. Skill-tree + SC plumbing
/// into the ratio are TODO.
/// </summary>
public sealed class RadiantSpear : RecursiveDamageSplashSkillImpl
{
    public RadiantSpear() : base(SkillIds.IG_RADIANT_SPEAR) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 3500 + 1150 * skillLevel) + 5 * src.Stats.Pow;
}
