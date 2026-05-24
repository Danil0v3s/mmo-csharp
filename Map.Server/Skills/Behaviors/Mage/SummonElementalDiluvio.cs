using Map.Server.Elemental;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_SUMMON_ELEMENTAL_DILUVIO — Elemental Master Summon Diluvio
/// (skill.cpp:EM_SUMMON_ELEMENTAL_DILUVIO arm). Requires the caster's
/// bound elemental to be <see cref="ElementalClassIds.AquaL"/>;
/// promotes that L-tier into <see cref="ElementalClassIds.Diluvio"/>
/// (rAthena's elemental_delete + elemental_create on the target
/// class), then applies SC_SUMMON_ELEMENTAL_DILUVIO.
/// </summary>
public sealed class SummonElementalDiluvio : SkillImpl
{
    public SummonElementalDiluvio() : base(SkillIds.EM_SUMMON_ELEMENTAL_DILUVIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (pc.ActiveElementalClassId != ElementalClassIds.AquaL) return;
        ctx.Elemental?.Create(pc, ElementalClassIds.Diluvio, ElementalClassIds.DefaultLifetimeMs);
        ctx.Sc?.Start(src, StatusType.SummonElementalDiluvio, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
    }
}
