using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SUMMON_AQUA — Sorcerer Summon Water Spirit (Aqua). Manual port of
/// <c>rathena-fork/src/map/skills/mage/summonwaterspiritaqua.cpp</c>.
/// Same shape as <see cref="SummonEarthSpiritTera"/>.
/// </summary>
public sealed class SummonWaterSpiritAqua : SkillImpl
{
    public SummonWaterSpiritAqua() : base(SkillIds.SO_SUMMON_AQUA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // Deferred: bound-elemental subsystem not ported — elemental_create AQUA-tier swap.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
