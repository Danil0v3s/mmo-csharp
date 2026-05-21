using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_VERDURETRAP — Ranger Verdure Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/verduretrap.cpp</c>.
/// Elemental conversion trap; drops a ground unit.
/// </summary>
public sealed class VerdureTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public VerdureTrap() : base(SkillIds.RA_VERDURETRAP) { }

    public VerdureTrap(ISkillUnitService? units = null) : base(SkillIds.RA_VERDURETRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
