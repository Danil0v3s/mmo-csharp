using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_PROVOCATION — Provoke target (force aggro).</summary>
public sealed class Provocation : SkillImpl
{
    public Provocation() : base(SkillIds.NPC_PROVOCATION) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Provoke, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
