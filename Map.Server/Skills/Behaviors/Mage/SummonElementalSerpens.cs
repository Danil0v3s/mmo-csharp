using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_SERPENS — Elemental Master Summon Serpens
/// (skill.cpp:EM_SUMMON_ELEMENTAL_SERPENS arm). Requires any L-tier
/// elemental active (Agni_L / Aqua_L / Ventus_L / Tera_L); promotes
/// to <see cref="ElementalClassIds.Serpens"/>.
/// </summary>
public sealed class SummonElementalSerpens : SkillImpl
{
    public SummonElementalSerpens() : base(SkillIds.EM_SUMMON_ELEMENTAL_SERPENS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (!ElementalClassIds.IsLTier(pc.ActiveElementalClassId)) return;
        ctx.Elemental?.Create(pc, ElementalClassIds.Serpens, ElementalClassIds.DefaultLifetimeMs);
        ctx.Sc?.Start(src, StatusType.SummonElementalSerpens, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
