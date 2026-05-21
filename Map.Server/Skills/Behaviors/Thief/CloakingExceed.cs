using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// GC_CLOAKINGEXCEED — Cloaking Exceed. Manual port of
/// <c>rathena-fork/src/map/skills/thief/cloakingexceed.cpp</c>.
/// Toggles SC_CLOAKINGEXCEED on the target. Failure refunds the cost.
/// </summary>
public sealed class CloakingExceed : SkillImpl
{
    public CloakingExceed() : base(SkillIds.GC_CLOAKINGEXCEED) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Cloakingexceed) != null)
        {
            ctx.Sc.End(target, StatusType.Cloakingexceed);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Cloakingexceed, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
