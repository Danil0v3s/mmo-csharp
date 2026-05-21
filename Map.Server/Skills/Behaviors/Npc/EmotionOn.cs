using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_EMOTION_ON — Mob emoticon broadcast. Animation only.</summary>
public sealed class EmotionOn : SkillImpl
{
    public EmotionOn() : base(SkillIds.NPC_EMOTION_ON) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
