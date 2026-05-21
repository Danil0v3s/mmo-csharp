using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_COBALTTRAP — Ranger Cobalt Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/cobalttrap.cpp</c>.
/// Drops the elemental conversion trap at the cast XY.
/// </summary>
public sealed class CobaltTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public CobaltTrap() : base(SkillIds.RA_COBALTTRAP) { }

    public CobaltTrap(ISkillUnitService? units = null) : base(SkillIds.RA_COBALTTRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
