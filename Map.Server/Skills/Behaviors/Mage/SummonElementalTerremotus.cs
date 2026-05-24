using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_TERREMOTUS — Elemental Master Summon Terremotus.
/// Promotes Tera_L → TERREMOTUS and buffs the caster.
/// </summary>
public sealed class SummonElementalTerremotus : SkillImpl
{
    public SummonElementalTerremotus() : base(SkillIds.EM_SUMMON_ELEMENTAL_TERREMOTUS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — TERA_L → TERREMOTUS upgrade.
        ctx.Sc?.Start(src, StatusType.SummonElementalTerremotus, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
