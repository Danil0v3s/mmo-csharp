using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_GIANTGROWTH — Rune Knight Giant Growth. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/giantgrowth.cpp</c>.
/// Requires RK_RUNEMASTERY ≥ 1 (TODO). Starts SC_GIANTGROWTH.
/// </summary>
public sealed class GiantGrowth : SkillImpl
{
    public GiantGrowth() : base(SkillIds.RK_GIANTGROWTH) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: gate on pc_checkskill(sd, RK_RUNEMASTERY) >= 1.
        ctx.Sc?.Start(target, StatusType.Giantgrowth, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
