using Map.Server.Entities;
using Map.Server.Status.StatusOps;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// DC_UGLYDANCE — Dancer Hip Shaker (Ugly Dance). Manual port of
/// <c>rathena-fork/src/map/skills/archer/hipshaker.cpp</c>.
/// Drains SP from each victim. Renewal: <c>2*lv + 10</c>.
/// </summary>
public sealed class HipShaker : SkillImpl
{
    private readonly ISkillUnitService? _units;
    private readonly IStatusOpsService? _statusOps;

    public HipShaker() : base(SkillIds.DC_UGLYDANCE) { }

    public HipShaker(ISkillUnitService? units = null, IStatusOpsService? statusOps = null) : base(SkillIds.DC_UGLYDANCE)
    {
        _units = units;
        _statusOps = statusOps;
    }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => _units?.Place(src, SkillId, skillLevel, x, y);

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        _statusOps?.Zap(target, 0, 2 * skillLevel + 10);
    }
}
