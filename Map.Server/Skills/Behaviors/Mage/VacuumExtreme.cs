using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SO_VACUUM_EXTREME — Sorcerer Vacuum Extreme. Ground unit placement.</summary>
public sealed class VacuumExtreme : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public VacuumExtreme() : base(SkillIds.SO_VACUUM_EXTREME) { }
    public VacuumExtreme(ISkillUnitService? units = null) : base(SkillIds.SO_VACUUM_EXTREME) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
