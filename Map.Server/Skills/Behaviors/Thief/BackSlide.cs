using Map.Server.Entities;
using Map.Server.Movement.UnitOps;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// TF_BACKSLIDING — Back Slide. Manual port of
/// <c>rathena-fork/src/map/skills/thief/backslide.cpp</c>.
/// Reads the target's facing direction (rAthena <c>unit_getdir</c>) and
/// knocks it 5 cells in that direction via
/// <see cref="IUnitOpsService.BlownBy"/> with the IGNORE_NO_KNOCKBACK
/// semantic. The 200 ms unstoppable window (rAthena
/// <c>ud->endure_tick = tick + 200</c>) is folded into the engine-side
/// endure SC pump.
/// </summary>
public sealed class BackSlide : SkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    /// <summary>rAthena <c>skill_get_blewcount(TF_BACKSLIDING, lv)</c> —
    /// fixed 5-cell knockback at every skill level.</summary>
    private const int BLEW_COUNT = 5;

    public BackSlide() : base(SkillIds.TF_BACKSLIDING) { }

    public BackSlide(IUnitOpsService? unitOps = null) : base(SkillIds.TF_BACKSLIDING)
    {
        _unitOps = unitOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Read facing direction via the canonical UnitOps.GetDir helper
        // so the slide ends up in the direction rAthena's
        // unit_getdir(bl) reports.
        var dir = _unitOps?.GetDir(target) ?? 0;
        _unitOps?.BlownBy(target, dir, BLEW_COUNT);
    }
}
