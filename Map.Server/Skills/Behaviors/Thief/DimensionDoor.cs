using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_DIMENSIONDOOR — Dimension Door. Manual port of
/// <c>rathena-fork/src/map/skills/thief/dimensiondoor.cpp</c>.
/// Drops a Dimension Door cell at the targeted tile.
/// </summary>
public sealed class DimensionDoor : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public DimensionDoor() : base(SkillIds.SC_DIMENSIONDOOR) { }

    public DimensionDoor(ISkillUnitService? units = null) : base(SkillIds.SC_DIMENSIONDOOR)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
