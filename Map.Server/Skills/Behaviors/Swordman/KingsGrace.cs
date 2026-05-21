using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// LG_KINGS_GRACE — Royal Guard King's Grace. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/kingsgrace.cpp</c>.
/// Places the protection field at the cast XY.
/// </summary>
public sealed class KingsGrace : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public KingsGrace() : base(SkillIds.LG_KINGS_GRACE) { }

    public KingsGrace(ISkillUnitService? units = null) : base(SkillIds.LG_KINGS_GRACE)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
