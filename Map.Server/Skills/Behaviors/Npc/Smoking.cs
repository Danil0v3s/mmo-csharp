using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_SMOKING — Mob smoke emote (cosmetic).</summary>
public sealed class Smoking : SkillImpl
{
    public Smoking() : base(SkillIds.NPC_SMOKING) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
