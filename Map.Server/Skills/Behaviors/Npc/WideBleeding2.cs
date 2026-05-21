using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDEBLEEDING2 — high-rate variant. 100% SC_BLEEDING on splash hit.</summary>
public sealed class WideBleeding2 : SkillImpl
{
    public WideBleeding2() : base(SkillIds.NPC_WIDEBLEEDING2) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Bleeding, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 30_000, src);
    }
}
