using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// RG_GRAFFITI — Scribble (Graffiti). Manual port of
/// <c>rathena-fork/src/map/skills/thief/scribble.cpp</c>.
/// Drops a Graffiti cell at the targeted tile.
/// </summary>
public sealed class Scribble : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public Scribble() : base(SkillIds.RG_GRAFFITI) { }

    public Scribble(ISkillUnitService? units = null) : base(SkillIds.RG_GRAFFITI)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
