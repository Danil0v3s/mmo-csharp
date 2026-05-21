using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_FORTUNEKISS — Dancer Lady Luck (Fortune Kiss). Manual port of
/// <c>rathena-fork/src/map/skills/archer/ladyluck.cpp</c>.
/// Drops the song ground unit (legacy path).
/// </summary>
public sealed class LadyLuck : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public LadyLuck() : base(SkillIds.DC_FORTUNEKISS) { }

    public LadyLuck(ISkillUnitService? units = null) : base(SkillIds.DC_FORTUNEKISS)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
