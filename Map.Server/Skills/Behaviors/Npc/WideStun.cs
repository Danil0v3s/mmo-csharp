using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDESTUN — 100% SC_STUN on splash hit.</summary>
public sealed class WideStun : SkillImpl
{
    public WideStun() : base(SkillIds.NPC_WIDESTUN) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, val2: (int)src.Id.Value, 0, 0, durationMs: 20_000, src);
    }
}
