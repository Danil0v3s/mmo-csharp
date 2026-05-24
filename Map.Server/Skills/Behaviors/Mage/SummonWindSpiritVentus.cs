using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_VENTUS — Sorcerer Summon Wind Spirit (Ventus). Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonwindspiritventus.cpp</c>.
/// Same shape as <see cref="SummonEarthSpiritTera"/>.
/// </summary>
public sealed class SummonWindSpiritVentus : SkillImpl
{
    public SummonWindSpiritVentus() : base(SkillIds.SO_SUMMON_VENTUS) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — elemental_create VENTUS-tier swap.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
