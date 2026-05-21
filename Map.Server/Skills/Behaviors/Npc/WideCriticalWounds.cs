using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDECRITICALWOUND — 100% SC_CRITICALWOUND on splash hit.</summary>
public sealed class WideCriticalWounds : SkillImpl
{
    public WideCriticalWounds() : base(SkillIds.NPC_WIDECRITICALWOUND) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Criticalwound, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
