using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SKIDTRAP — Hunter Skid Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/skidtrap.cpp</c>.
/// Drops the trap at the cast XY.
/// </summary>
public sealed class SkidTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public SkidTrap() : base(SkillIds.HT_SKIDTRAP) { }

    public SkidTrap(ISkillUnitService? units = null) : base(SkillIds.HT_SKIDTRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
