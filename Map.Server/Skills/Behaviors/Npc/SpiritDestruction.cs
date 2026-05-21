using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_MENTALBREAKER — SP destruction (drain SP %).</summary>
public sealed class SpiritDestruction : SkillImpl
{
    public SpiritDestruction() : base(SkillIds.NPC_MENTALBREAKER) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (target is PlayerEntity p)
            p.Sp = System.Math.Max(0, p.Sp - p.MaxSp * (50 + 10 * skillLevel) / 100);
    }
}
