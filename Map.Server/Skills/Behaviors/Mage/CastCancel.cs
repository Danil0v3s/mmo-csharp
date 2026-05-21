using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CASTCANCEL — Sage Cast Cancel. Interrupts caster's own current
/// cast. SP cost gates: <c>(90 - (lv-1)*20) %</c> of the cancelled
/// skill's SP cost.
/// </summary>
public sealed class CastCancel : SkillImpl
{
    public CastCancel() : base(SkillIds.SA_CASTCANCEL) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: unit_skillcastcancel(src, 1) — needs SkillCastService.Cancel.
        // TODO: SP refund of (90 - (lv-1)*20)% of the cancelled skill's SP cost.
    }
}
