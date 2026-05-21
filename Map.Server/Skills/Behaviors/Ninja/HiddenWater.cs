using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_SUITON — Hidden Water. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/hiddenwater.cpp</c>.
/// Drops a Suiton unit at the targeted cell.
/// </summary>
public sealed class HiddenWater : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public HiddenWater() : base(SkillIds.NJ_SUITON) { }

    public HiddenWater(ISkillUnitService? units = null) : base(SkillIds.NJ_SUITON)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
