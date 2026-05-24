using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_CASTCANCEL — Sage Cast Cancel (skill.cpp:SA_CASTCANCEL arm).
/// Interrupts the caster's own in-flight cast via
/// <see cref="ISkillCastService.CancelCast"/>; rAthena's
/// <c>unit_skillcastcancel(src, 1)</c>. SP refund (90 − (lv−1)·20 %)
/// rides on the skill-requirement-consume hook; the cancellation half
/// lands here.
/// </summary>
public sealed class CastCancel : SkillImpl
{
    public CastCancel() : base(SkillIds.SA_CASTCANCEL) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Cast?.CancelCast(src.Id);
    }
}
