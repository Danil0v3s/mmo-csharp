using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SO_WARMER — Sorcerer Warmer. Ground unit placement (Frost-cure HoT zone).</summary>
public sealed class Warmer : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Warmer() : base(SkillIds.SO_WARMER) { }
    public Warmer(ISkillUnitService? units = null) : base(SkillIds.SO_WARMER) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
