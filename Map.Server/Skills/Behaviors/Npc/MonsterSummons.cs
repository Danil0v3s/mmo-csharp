using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SUMMONMONSTER — Mob spawns reinforcement mobs. Mob spawn TODO.</summary>
public sealed class MonsterSummons : SkillImpl
{
    public MonsterSummons() : base(SkillIds.NPC_SUMMONMONSTER) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
