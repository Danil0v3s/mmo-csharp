using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DANCINGBLADE — Schedules NPC_DANCINGBLADE_ATK via skill timer. Timer dispatch TODO.</summary>
public sealed class DancingBlade : SkillImpl
{
    public DancingBlade() : base(SkillIds.NPC_DANCINGBLADE) { }
    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
