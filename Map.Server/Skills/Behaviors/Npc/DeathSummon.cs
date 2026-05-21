using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_DEATHSUMMON — Summon a Death Servant. Mob spawn TODO.</summary>
public sealed class DeathSummon : SkillImpl
{
    public DeathSummon() : base(SkillIds.NPC_DEATHSUMMON) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
