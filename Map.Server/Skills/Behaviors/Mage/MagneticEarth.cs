using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_LANDPROTECTOR — Sage Magnetic Earth / Land Protector. Ground unit placement.</summary>
public sealed class MagneticEarth : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public MagneticEarth() : base(SkillIds.SA_LANDPROTECTOR) { }
    public MagneticEarth(ISkillUnitService? units = null) : base(SkillIds.SA_LANDPROTECTOR) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
