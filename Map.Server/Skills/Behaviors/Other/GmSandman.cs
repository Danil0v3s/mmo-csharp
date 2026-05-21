using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// GM_SANDMAN — GM sleep toggle. Manual port of
/// <c>rathena-fork/src/map/skills/other/gmsandman.cpp</c>.
/// Toggles OPT1_SLEEP on the target.
/// </summary>
public sealed class GmSandman : SkillImpl
{
    public GmSandman() : base(SkillIds.GM_SANDMAN) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc?.Get(target, StatusType.Sleep) != null)
            ctx.Sc.End(target, StatusType.Sleep);
        else
            ctx.Sc?.Start(target, StatusType.Sleep, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
