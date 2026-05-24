using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_BUSTER — Elemental Master Elemental Buster. Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalbuster.cpp</c>.
///
/// <para>Requires a bound EM-tier elemental (ARDOR / DILUVIO / PROCELLA
/// / TERREMOTUS / SERPENS). Dispatches the matching elemental-specific
/// buster (Fire/Water/Wind/Ground/Poison) as a 6-tile splash. Elemental
/// binding + per-element dispatch is TODO until the EM-elemental
/// service lands; for now we only fail-fast if the caster isn't a
/// player.</para>
/// </summary>
public sealed class ElementalBuster : SkillImpl
{
    public ElementalBuster() : base(SkillIds.EM_ELEMENTAL_BUSTER) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd)
            return;
        // Deferred: bound-elemental subsystem not ported — class lookup + per-element
        // dispatch to EM_ELEMENTAL_BUSTER_{FIRE,WATER,WIND,GROUND,POISON} requires it.
        // For now, fail the cast so the requirement-refund path runs.
        ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
    }
}
