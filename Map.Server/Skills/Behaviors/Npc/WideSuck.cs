using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDESUCK — 100% SC_BLOODSUCKER (drain HP) on splash hit.</summary>
public sealed class WideSuck : SkillImpl
{
    public WideSuck() : base(SkillIds.NPC_WIDESUCK) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Bloodsucker, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
    }
}
