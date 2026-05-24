using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_MILLENNIUMSHIELD — Rune Knight Millennium Shield. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/milleniumshield.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 9 (TODO). Applies SC_MILLENNIUMSHIELD.
/// </summary>
public sealed class MilleniumShield : SkillImpl
{
    public MilleniumShield() : base(SkillIds.RK_MILLENNIUMSHIELD) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: rAthena gates this on pc_checkskill(sd, RK_RUNEMASTERY) >= 9;
        // RK_RUNEMASTERY is not yet in SkillIds. Once added, gate via
        // ctx.PlayerSkill?.CheckSkill(sd, SkillIds.RK_RUNEMASTERY) >= 9.
        ctx.Sc?.Start(target, StatusType.Millenniumshield, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
