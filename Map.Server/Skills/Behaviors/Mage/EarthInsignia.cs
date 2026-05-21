using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_EARTH_INSIGNIA — Sorcerer Earth Insignia. Manual port of
/// <c>rathena-fork/src/map/skills/mage/earthinsignia.cpp</c>.
/// Single-line body: drop the Earth Insignia ground unit.
/// </summary>
public sealed class EarthInsignia : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public EarthInsignia() : base(SkillIds.SO_EARTH_INSIGNIA) { }
    public EarthInsignia(ISkillUnitService? units = null) : base(SkillIds.SO_EARTH_INSIGNIA) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
