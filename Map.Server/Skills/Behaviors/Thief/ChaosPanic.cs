using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// SC_CHAOSPANIC — Chaos Panic. Manual port of
/// <c>rathena-fork/src/map/skills/thief/chaospanic.cpp</c>.
/// Drops a Chaos Panic cell at the targeted tile.
/// </summary>
public sealed class ChaosPanic : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ChaosPanic() : base(SkillIds.SC_CHAOSPANIC) { }

    public ChaosPanic(ISkillUnitService? units = null) : base(SkillIds.SC_CHAOSPANIC)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
