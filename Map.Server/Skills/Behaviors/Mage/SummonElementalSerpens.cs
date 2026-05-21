using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_SERPENS — Elemental Master Summon Serpens.
/// Promotes any level-3 elemental (AGNI/AQUA/VENTUS/TERA_L) → SERPENS
/// and buffs the caster.
/// </summary>
public sealed class SummonElementalSerpens : SkillImpl
{
    public SummonElementalSerpens() : base(SkillIds.EM_SUMMON_ELEMENTAL_SERPENS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity) return;
        // TODO: bound-elemental upgrade any L3 → SERPENS.
        ctx.Sc?.Start(src, StatusType.SummonElementalSerpens, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
