using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Summoner;

/// <summary>
/// SU_HISS — Summoner Hiss. Manual port of
/// <c>rathena-fork/src/map/skills/summoner/hiss.cpp</c>. Applies SC_HISS;
/// party splash is TODO.
/// </summary>
public sealed class Hiss : SkillImpl
{
    public Hiss() : base(SkillIds.SU_HISS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Hiss, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
