using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_STOP — Target SC_STOP debuff (cannot move).</summary>
public sealed class Stop : SkillImpl
{
    public Stop() : base(SkillIds.NPC_STOP) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Stop, val1: skillLevel, 0, 0, 0, durationMs: 3_000 * skillLevel, src);
    }
}
