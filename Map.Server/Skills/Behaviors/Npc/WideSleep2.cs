using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDESLEEP2 — high-rate variant. 100% SC_SLEEP on splash hit.</summary>
public sealed class WideSleep2 : SkillImpl
{
    public WideSleep2() : base(SkillIds.NPC_WIDESLEEP2) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Sleep, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
