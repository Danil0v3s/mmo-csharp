using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_REFRESH — Rune Knight Refresh. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/refresh.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 8 (TODO). Applies SC_REFRESH.
/// </summary>
public sealed class Refresh : SkillImpl
{
    public Refresh() : base(SkillIds.RK_REFRESH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: rAthena gates this on pc_checkskill(sd, RK_RUNEMASTERY) >= 8;
        // RK_RUNEMASTERY is not yet in SkillIds. Gate via
        // ctx.PlayerSkill?.CheckSkill(sd, SkillIds.RK_RUNEMASTERY) >= 8 once added.
        ctx.Sc?.Start(target, StatusType.Refresh, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
