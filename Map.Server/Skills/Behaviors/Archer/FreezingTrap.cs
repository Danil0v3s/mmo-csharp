using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_FREEZINGTRAP — Hunter Freezing Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/freezingtrap.cpp</c>.
/// Trap unit + on-hit SC_FREEZE at 100 %.
/// </summary>
public sealed class FreezingTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public FreezingTrap() : base(SkillIds.HT_FREEZINGTRAP) { }

    public FreezingTrap(ISkillUnitService? units = null) : base(SkillIds.HT_FREEZINGTRAP)
    {
        _units = units;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Freeze, val1: skillLevel, 0, 0, 0, durationMs: 8000, src);
    }
}
