using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// HW_GRAVITATION — High Wizard Gravitation Field. Manual port of
/// <c>rathena-fork/src/map/skills/mage/gravitationfield.cpp</c>.
///
/// <para>Renewal: drops the gravitation ground unit at the target XY.
/// Ratio: <c>+(-100 + 100*lv)</c>. Per-tick damage is driven by the
/// SkillUnit engine via skill_unit_db.</para>
/// </summary>
public sealed class GravitationField : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public GravitationField() : base(SkillIds.HW_GRAVITATION) { }

    public GravitationField(ISkillUnitService? units = null) : base(SkillIds.HW_GRAVITATION)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 100 * skillLevel);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
