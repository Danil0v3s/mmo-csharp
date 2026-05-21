using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// WM_LERADS_DEW — Minstrel/Wanderer Lerads Dew. Manual port of
/// <c>rathena-fork/src/map/skills/archer/leradsdew.cpp</c>. Party-wide
/// MaxHP buff (WM_LESSON-scaled). Lands on the target.
/// </summary>
public sealed class LeradsDew : SkillImpl
{
    public LeradsDew() : base(SkillIds.WM_LERADS_DEW) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Leradsdew, val1: skillLevel, val2: 5, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
