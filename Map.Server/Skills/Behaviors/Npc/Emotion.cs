using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_EMOTION — Mob emoticon broadcast. Animation only.</summary>
public sealed class Emotion : SkillImpl
{
    public Emotion() : base(SkillIds.NPC_EMOTION) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
