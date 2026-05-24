using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_ESTIMATION — Mercenary Sense. Manual port of
/// <c>rathena-fork/src/map/skills/mercenary/mercenary_sense.cpp</c>.
/// Sends estimation packet for the targeted mob to the mercenary's
/// master. Master lookup deferred (mercenary master-link not yet on
/// MercenaryEntity).
/// </summary>
public sealed class MercenarySense : SkillImpl
{
    public MercenarySense() : base(SkillIds.MER_ESTIMATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity) return; // Only works on mobs.
        // Deferred: clif_skill_estimation(master, dstmd) — needs mercenary→master link plumbed into ctx.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
