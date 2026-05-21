using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>NPC_WIDESOULDRAIN — Splash SP drain (10 * skillLevel %) on splash hit. Splash iteration TODO.</summary>
public sealed class WideSoulDrain : SkillImpl
{
    public WideSoulDrain() : base(SkillIds.NPC_WIDESOULDRAIN) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // status_percent_damage(src, target, 0, -100 * skillLevel, false); — drain SP%
        if (target is PlayerEntity p)
            p.Sp = System.Math.Max(0, p.Sp - p.MaxSp * skillLevel / 10);
    }
}
