using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SO_WATER_INSIGNIA — Sorcerer Water Insignia. Ground unit placement.</summary>
public sealed class WaterInsignia : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WaterInsignia() : base(SkillIds.SO_WATER_INSIGNIA) { }
    public WaterInsignia(ISkillUnitService? units = null) : base(SkillIds.SO_WATER_INSIGNIA) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
