using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>SA_VOLCANO — Sage Volcano. Element field (Fire ATK boost zone). Element-field overlap TODO.</summary>
public sealed class Volcano : SkillImpl
{
    private readonly ISkillUnitService? _units;
    public Volcano() : base(SkillIds.SA_VOLCANO) { }
    public Volcano(ISkillUnitService? units = null) : base(SkillIds.SA_VOLCANO) => _units = units;
    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
