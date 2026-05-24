using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_TERREMOTUS — Elemental Master Summon Terremotus
/// (skill.cpp:EM_SUMMON_ELEMENTAL_TERREMOTUS arm). Requires
/// <see cref="ElementalClassIds.TeraL"/>; promotes to
/// <see cref="ElementalClassIds.Terremotus"/>.
/// </summary>
public sealed class SummonElementalTerremotus : SkillImpl
{
    public SummonElementalTerremotus() : base(SkillIds.EM_SUMMON_ELEMENTAL_TERREMOTUS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId != ElementalClassIds.TeraL) return;
        ctx.Elemental?.Create(pc, ElementalClassIds.Terremotus, ElementalClassIds.DefaultLifetimeMs);
        ctx.Sc?.Start(src, StatusType.SummonElementalTerremotus, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
