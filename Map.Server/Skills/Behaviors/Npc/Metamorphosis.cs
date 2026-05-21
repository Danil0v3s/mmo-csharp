using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_METAMORPHOSIS — Mob transformation. Transformation TODO.</summary>
public sealed class Metamorphosis : SkillImpl
{
    public Metamorphosis() : base(SkillIds.NPC_METAMORPHOSIS) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
