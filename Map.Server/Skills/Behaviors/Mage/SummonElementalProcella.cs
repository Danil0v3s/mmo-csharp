using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_PROCELLA — Elemental Master Summon Procella.
/// Promotes Ventus_L → PROCELLA and buffs the caster.
/// </summary>
public sealed class SummonElementalProcella : SkillImpl
{
    public SummonElementalProcella() : base(SkillIds.EM_SUMMON_ELEMENTAL_PROCELLA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — VENTUS_L → PROCELLA upgrade.
        ctx.Sc?.Start(src, StatusType.SummonElementalProcella, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
