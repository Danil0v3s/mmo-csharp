using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Other;

/// <summary>
/// ALL_ODINS_RECALL — Odin's Recall (teleport-scroll variant). Manual
/// port of <c>rathena-fork/src/map/skills/other/odinsrecall.cpp</c>.
/// Lv 1 → random warp on map, Lv 2 → savepoint warp, Lv 3 → silent
/// savepoint warp. Warp pipeline deferred (savepoint map persistence
/// not yet plumbed); we land the animation.
/// </summary>
public sealed class OdinsRecall : SkillImpl
{
    public OdinsRecall() : base(SkillIds.ALL_ODINS_RECALL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: pc_randomwarp / pc_setpos to savepoint per skillLevel — needs savepoint map persistence on PlayerEntity.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
