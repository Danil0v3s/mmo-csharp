using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WH_FLAMETRAP — Wind Hawk Flame Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/flametrap.cpp</c>.
/// Same shape as <see cref="DeepBlindTrap"/>.
/// </summary>
public sealed class FlameTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public FlameTrap() : base(SkillIds.WH_FLAMETRAP) { }

    public FlameTrap(ISkillUnitService? units = null) : base(SkillIds.WH_FLAMETRAP)
    {
        _units = units;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 850 * skillLevel + 5 * src.Stats.Con);
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
