using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_MAELSTROM — Maelstrom. Manual port of
/// <c>rathena-fork/src/map/skills/thief/maelstrom.cpp</c>.
/// Drops a Maelstrom cell at the targeted tile.
/// </summary>
public sealed class Maelstrom : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Maelstrom() : base(SkillIds.SC_MAELSTROM) { }

    public Maelstrom(ISkillUnitService? units = null) : base(SkillIds.SC_MAELSTROM)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
