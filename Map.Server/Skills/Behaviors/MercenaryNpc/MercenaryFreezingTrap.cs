using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MA_FREEZINGTRAP — Mercenary Freezing Trap. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_freezingtrap.cpp</c>.
/// Drops the unit at the cast XY; on hit 100% SC_FREEZE.
/// </summary>
public sealed class MercenaryFreezingTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public MercenaryFreezingTrap() : base(SkillIds.MA_FREEZINGTRAP) { }

    public MercenaryFreezingTrap(ISkillUnitService? units = null) : base(SkillIds.MA_FREEZINGTRAP)
    {
        _units = units;
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);
}
