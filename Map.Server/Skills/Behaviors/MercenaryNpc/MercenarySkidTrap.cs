using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_SKIDTRAP — Mercenary Skid Trap. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_skidtrap.cpp</c>.
/// Drops the trap at the cast XY.
/// </summary>
public sealed class MercenarySkidTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MercenarySkidTrap() : base(SkillIds.MA_SKIDTRAP) { }

    public MercenarySkidTrap(ISkillUnitService? units = null) : base(SkillIds.MA_SKIDTRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
