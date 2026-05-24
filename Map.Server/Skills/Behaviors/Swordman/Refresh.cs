using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_REFRESH — Rune Knight Refresh. rAthena gates this on
/// <c>pc_checkskill(sd, RK_RUNEMASTERY) &gt;= 8</c> (skill.cpp:11293).
/// Applies SC_REFRESH; without the rune-mastery prereq the cast
/// fails silently (rAthena returns 0 from skill_castend_nodamage).
/// </summary>
public sealed class Refresh : SkillImpl
{
    public Refresh() : base(SkillIds.RK_REFRESH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        if ((ctx.PlayerSkill?.CheckSkill(pc, SkillIds.RK_RUNEMASTERY) ?? 0) < 8) return;
        ctx.Sc?.Start(target, StatusType.Refresh, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
