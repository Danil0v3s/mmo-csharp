using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_DONTFORGETME — Dancer Slow Grace (Don't Forget Me). Manual
/// port of <c>rathena-fork/src/map/skills/archer/slowgrace.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class SlowGrace : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SlowGrace() : base(SkillIds.DC_DONTFORGETME) { }

    public SlowGrace(ISkillUnitService? units = null) : base(SkillIds.DC_DONTFORGETME)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
