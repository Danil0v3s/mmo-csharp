using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDECONFUSE2 — high-rate variant. 100% SC_CONFUSION on splash hit.</summary>
public sealed class WideConfusion2 : SkillImpl
{
    public WideConfusion2() : base(SkillIds.NPC_WIDECONFUSE2) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
    }
}
