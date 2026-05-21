using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_LICK — Drain 100 SP; 20*lv % SC_STUN on target.</summary>
public sealed class Lick : SkillImpl
{
    public Lick() : base(SkillIds.NPC_LICK) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // status_zap(target, 0, 100) — drain 100 SP
        if (target is PlayerEntity p)
            p.Sp = System.Math.Max(0, p.Sp - 100);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (System.Random.Shared.Next(100) < skillLevel * 20)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
