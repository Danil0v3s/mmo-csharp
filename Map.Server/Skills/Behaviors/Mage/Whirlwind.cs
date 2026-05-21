using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_VIOLENTGALE — Sage Whirlwind (Violent Gale). Element field (Wind ATK boost zone).</summary>
public sealed class Whirlwind : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Whirlwind() : base(SkillIds.SA_VIOLENTGALE) { }
    public Whirlwind(ISkillUnitService? units = null) : base(SkillIds.SA_VIOLENTGALE) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
