using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SUMMONSLAVE — Mob spawns slave mobs. Mob spawn TODO.</summary>
public sealed class FollowerSummons : SkillImpl
{
    public FollowerSummons() : base(SkillIds.NPC_SUMMONSLAVE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
