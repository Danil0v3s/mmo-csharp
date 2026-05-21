using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_ABUNDANCE — Rune Knight Abundance. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/abundance.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 6 (skill-tree check is TODO). Applies
/// SC_ABUNDANCE to the target on success or shows a skill-fail.
/// </summary>
public sealed class Abundance : SkillImpl
{
    public Abundance() : base(SkillIds.RK_ABUNDANCE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd) return;
        // TODO: gate on pc_checkskill(sd, RK_RUNEMASTERY) >= 6.
        ctx.Sc?.Start(target, StatusType.Abundance, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
