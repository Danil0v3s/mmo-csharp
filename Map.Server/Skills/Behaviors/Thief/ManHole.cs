using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_MANHOLE — Man Hole. Manual port of
/// <c>rathena-fork/src/map/skills/thief/manhole.cpp</c>.
/// Drops a Man Hole cell at the targeted tile.
/// </summary>
public sealed class ManHole : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ManHole() : base(SkillIds.SC_MANHOLE) { }

    public ManHole(ISkillUnitService? units = null) : base(SkillIds.SC_MANHOLE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
