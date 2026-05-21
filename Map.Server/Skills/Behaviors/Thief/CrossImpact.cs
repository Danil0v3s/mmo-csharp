using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CROSSIMPACT — Cross Impact. Manual port of
/// <c>rathena-fork/src/map/skills/thief/crossimpact.cpp</c>.
/// Ratio <c>+(-100 + 1400 + 150*lv)</c>. Caster slides behind target
/// (TODO — skill_check_unit_movepos).
/// </summary>
public sealed class CrossImpact : WeaponSkillImpl
{
    public CrossImpact() : base(SkillIds.GC_CROSSIMPACT) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 1400 + 150 * skillLevel);
}
