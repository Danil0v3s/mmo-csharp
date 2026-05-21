using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_VENOMDUST — Venom Dust. Manual port of
/// <c>rathena-fork/src/map/skills/thief/venomdust.cpp</c>.
/// Drops a venom dust cell at (x, y).
/// </summary>
public sealed class VenomDust : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public VenomDust() : base(SkillIds.AS_VENOMDUST) { }

    public VenomDust(ISkillUnitService? units = null) : base(SkillIds.AS_VENOMDUST)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
