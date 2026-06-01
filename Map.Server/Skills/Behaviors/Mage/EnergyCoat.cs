using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// MG_ENERGYCOAT — Mage Energy Coat. Manual port of
/// <c>rathena-fork/src/map/skills/mage/energycoat.cpp</c> (the fork
/// body is empty — base StatusSkillImpl provides the standard SC
/// apply pipeline). We override CastendNoDamageId to wire the
/// SC_ENERGYCOAT apply directly so it works without skill_db SC
/// metadata.
/// </summary>
public sealed class EnergyCoat : StatusSkillImpl
{
    public EnergyCoat() : base(SkillIds.MG_ENERGYCOAT) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: Energy Coat is permanent until cancelled / dispelled.
        // The SC handler reads caster SP and applies the per-tier damage
        // reduction on incoming hits.
        ctx.Sc?.Start(target, StatusType.Energycoat,
            val1: skillLevel, 0, 0, 0, durationMs: -1, src);
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
