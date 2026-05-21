using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDESIGHT — 100% SC_SIGHT on splash hit (uncloak).</summary>
public sealed class WideSight : SkillImpl
{
    public WideSight() : base(SkillIds.NPC_WIDESIGHT) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Sight, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
    }
}
