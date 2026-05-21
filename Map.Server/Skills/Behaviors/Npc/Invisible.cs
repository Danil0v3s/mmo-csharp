using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_INVISIBLE — Mob self-cloak (val4 = 6 for infinite cloak).</summary>
public sealed class Invisible : SkillImpl
{
    public Invisible() : base(SkillIds.NPC_INVISIBLE) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.Cloaking, val1: skillLevel, 0, 0, val4: 6, durationMs: 1_800_000, src);
    }
}
