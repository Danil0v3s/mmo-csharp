using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_CLAYMORETRAP — Hunter Claymore Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/claymoretrap.cpp</c>.
/// Drops the trap at the cast XY.
/// </summary>
public sealed class ClaymoreTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ClaymoreTrap() : base(SkillIds.HT_CLAYMORETRAP) { }

    public ClaymoreTrap(ISkillUnitService? units = null) : base(SkillIds.HT_CLAYMORETRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
