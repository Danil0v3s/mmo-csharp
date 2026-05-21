using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_MAGENTATRAP — Ranger Magenta Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/magentatrap.cpp</c>.
/// Elemental conversion trap; drops a ground unit.
/// </summary>
public sealed class MagentaTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MagentaTrap() : base(SkillIds.RA_MAGENTATRAP) { }

    public MagentaTrap(ISkillUnitService? units = null) : base(SkillIds.RA_MAGENTATRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
