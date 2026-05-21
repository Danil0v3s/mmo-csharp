using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// HT_SHOCKWAVE — Hunter Shockwave Trap. Manual port of
/// <c>rathena-fork/src/map/skills/archer/shockwavetrap.cpp</c>.
/// Drops trap; on-hit drains <c>(15*lv + 5)%</c> of victim's SP.
/// </summary>
public sealed class ShockwaveTrap : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly IStatusOpsService? _statusOps;

    public ShockwaveTrap() : base(SkillIds.HT_SHOCKWAVE) { }

    public ShockwaveTrap(ISkillUnitService? units = null, IStatusOpsService? statusOps = null) : base(SkillIds.HT_SHOCKWAVE)
    {
        _units = units;
        _statusOps = statusOps;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var pct = (sbyte)(15 * skillLevel + 5);
        _statusOps?.PercentDamage(src, target, 0, (sbyte)-pct, can_kill: false);
    }
}
