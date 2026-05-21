using Map.Server.Entities;
using Map.Server.Movement.UnitOps;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_WIND_STEP — Elemental Wind Step. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/windstep.cpp</c>.
/// Toggles SC_WIND_STEP on target + SC_WIND_STEP_OPTION on the
/// elemental, and on activation knocks the master back a few cells.
/// </summary>
public sealed class WindStep : SkillImpl
{
    private readonly IUnitOpsService? _unitOps;

    public WindStep() : base(SkillIds.EL_WIND_STEP) { }

    public WindStep(IUnitOpsService? unitOps = null) : base(SkillIds.EL_WIND_STEP)
    {
        _unitOps = unitOps;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.WindStep) != null
            || ctx.Sc?.Get(src, StatusType.WindStepOption) != null)
        {
            ctx.Sc.End(target, StatusType.WindStep);
            ctx.Sc.End(src, StatusType.WindStepOption);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // Push the master a random short distance — knockback parity.
        var dir = System.Random.Shared.Next(0, 8);
        var count = System.Random.Shared.Next(0, Math.Max(1, (int)skillLevel)) + 1;
        _unitOps?.BlownBy(target, dir, count);
        ctx.Sc?.Start(src, StatusType.WindStepOption, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Sc?.Start(target, StatusType.WindStep, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
    }
}
