using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_PROCELLA — Elemental Master Summon Procella
/// (skill.cpp:EM_SUMMON_ELEMENTAL_PROCELLA arm). Requires
/// <see cref="ElementalClassIds.VentusL"/>; promotes to
/// <see cref="ElementalClassIds.Procella"/>.
/// </summary>
public sealed class SummonElementalProcella : SkillImpl
{
    public SummonElementalProcella() : base(SkillIds.EM_SUMMON_ELEMENTAL_PROCELLA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId != ElementalClassIds.VentusL) return;
        ctx.Elemental?.Create(pc, ElementalClassIds.Procella, ElementalClassIds.DefaultLifetimeMs);
        ctx.Sc?.Start(src, StatusType.SummonElementalProcella, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
