using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.MercenaryNpc;

/// <summary>
/// MER_ESTIMATION — Mercenary Sense (skill.cpp:MER_ESTIMATION arm).
/// Sends the rAthena <c>clif_skill_estimation</c> packet (the mob
/// info panel) to the mercenary's master. Master lookup goes through
/// <see cref="Entity.MasterId"/> which the slave-AI binds at spawn.
/// </summary>
public sealed class MercenarySense : SkillImpl
{
    public MercenarySense() : base(SkillIds.MER_ESTIMATION) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (target is PlayerEntity) return; // Only works on mobs.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src.MasterId is not { } masterId) return;
        var master = ctx.Entities.Get(masterId);
        if (master is not PlayerEntity masterPc) return;
        ctx.Client?.BroadcastSkillEstimation(masterPc, target);
    }
}
