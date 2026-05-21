using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_FIRE_INSIGNIA — Sorcerer Fire Insignia. Manual port of
/// <c>rathena-fork/src/map/skills/mage/fireinsignia.cpp</c>.
/// Single-line body: drop the Fire Insignia ground unit.
/// </summary>
public sealed class FireInsignia : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public FireInsignia() : base(SkillIds.SO_FIRE_INSIGNIA) { }
    public FireInsignia(ISkillUnitService? units = null) : base(SkillIds.SO_FIRE_INSIGNIA) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
