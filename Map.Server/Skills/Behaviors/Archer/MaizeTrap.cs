using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// RA_MAIZETRAP — Ranger Maize Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/maizetrap.cpp</c>. Elemental
/// conversion trap; drops a ground unit.
/// </summary>
public sealed class MaizeTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MaizeTrap() : base(SkillIds.RA_MAIZETRAP) { }

    public MaizeTrap(ISkillUnitService? units = null) : base(SkillIds.RA_MAIZETRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
