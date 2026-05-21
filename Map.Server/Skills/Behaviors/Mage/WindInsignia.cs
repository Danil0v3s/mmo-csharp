using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SO_WIND_INSIGNIA — Sorcerer Wind Insignia. Ground unit placement.</summary>
public sealed class WindInsignia : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public WindInsignia() : base(SkillIds.SO_WIND_INSIGNIA) { }
    public WindInsignia(ISkillUnitService? units = null) : base(SkillIds.SO_WIND_INSIGNIA) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
