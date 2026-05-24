using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_ARDOR — Elemental Master Summon Ardor
/// (skill.cpp:EM_SUMMON_ELEMENTAL_ARDOR arm). Requires the caster's
/// bound elemental to be <see cref="ElementalClassIds.AgniL"/>;
/// promotes to <see cref="ElementalClassIds.Ardor"/> then applies
/// SC_SUMMON_ELEMENTAL_ARDOR.
/// </summary>
public sealed class SummonElementalArdor : SkillImpl
{
    public SummonElementalArdor() : base(SkillIds.EM_SUMMON_ELEMENTAL_ARDOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId != ElementalClassIds.AgniL) return;
        ctx.Elemental?.Create(pc, ElementalClassIds.Ardor, ElementalClassIds.DefaultLifetimeMs);
        ctx.Sc?.Start(src, StatusType.SummonElementalArdor, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
