using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_CHATTERING — Summoner Chattering. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/chattering.cpp</c>. Applies SC_CHATTERING.
/// </summary>
public sealed class Chattering : SkillImpl
{
    public Chattering() : base(SkillIds.SU_CHATTERING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Chattering, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
