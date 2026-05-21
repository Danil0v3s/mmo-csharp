using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Thief;

/// <summary>
/// AS_CLOAKING — Cloaking. Manual port of
/// <c>rathena-fork/src/map/skills/thief/cloaking.cpp</c>.
/// Toggles SC_CLOAKING on the target.
/// </summary>
public sealed class Cloaking : SkillImpl
{
    public Cloaking() : base(SkillIds.AS_CLOAKING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Cloaking) != null)
        {
            ctx.Sc.End(target, StatusType.Cloaking);
            ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
            return;
        }
        ctx.Sc?.Start(target, StatusType.Cloaking, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
