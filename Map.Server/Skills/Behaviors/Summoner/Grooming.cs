using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_GROOMING — Summoner Grooming. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/grooming.cpp</c>. Applies SC_GROOMING.
/// </summary>
public sealed class Grooming : SkillImpl
{
    public Grooming() : base(SkillIds.SU_GROOMING) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Grooming, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
