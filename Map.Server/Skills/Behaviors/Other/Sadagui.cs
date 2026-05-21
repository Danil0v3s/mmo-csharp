using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ECL_SADAGUI — Sadagui cleanse. Manual port of
/// <c>rathena-fork/src/map/skills/other/sadagui.cpp</c>.
/// Cleanses Stun / Confusion / Hallucination / Fear.
/// </summary>
public sealed class Sadagui : SkillImpl
{
    public Sadagui() : base(SkillIds.ECL_SADAGUI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.End(target, StatusType.Stun);
        ctx.Sc?.End(target, StatusType.Confusion);
        ctx.Sc?.End(target, StatusType.Hallucination);
        ctx.Sc?.End(target, StatusType.Fear);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
