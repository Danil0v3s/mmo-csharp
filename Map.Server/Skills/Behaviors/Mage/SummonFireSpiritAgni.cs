using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_AGNI — Sorcerer Summon Fire Spirit (Agni). Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonfirespiritagni.cpp</c>.
/// Same shape as <see cref="SummonEarthSpiritTera"/>.
/// </summary>
public sealed class SummonFireSpiritAgni : SkillImpl
{
    public SummonFireSpiritAgni() : base(SkillIds.SO_SUMMON_AGNI) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — elemental_create AGNI-tier swap.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
