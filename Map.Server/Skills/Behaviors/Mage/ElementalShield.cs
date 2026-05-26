using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ELEMENTAL_SHIELD — Sorcerer Elemental Shield. Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalshield.cpp</c>.
///
/// <para>Drops Safety Wall (lv+5) and Pneuma (lv 1) on the target's
/// tile, then drops the same pair at every same-map partymate's
/// current cell via <c>ctx.PartyMap.ForEachOnSameMap</c>. Consumes the
/// bound elemental (rAthena calls <c>elemental_delete</c> when the
/// skill state is <c>ST_ELEMENTALSPIRIT2</c>; the actual delete fires
/// when the elemental subsystem ports).</para>
/// </summary>
public sealed class ElementalShield : SkillImpl
{
    private readonly ISkillUnitService? _units;

    public ElementalShield() : base(SkillIds.SO_ELEMENTAL_SHIELD) { }

    public ElementalShield(ISkillUnitService? units = null) : base(SkillIds.SO_ELEMENTAL_SHIELD)
    {
        _units = units;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Safety Wall on target tile.
        _units?.Place(target, SkillIds.MG_SAFETYWALL, (ushort)(skillLevel + 5), target.X, target.Y);
        // Pneuma lv 1 on the same tile.
        _units?.Place(target, SkillIds.AL_PNEUMA, 1, target.X, target.Y);

        // rAthena party_foreachsamemap fan-out: drop the same Safety Wall +
        // Pneuma pair at every same-map partymate's current cell.
        if (src is PlayerEntity pcSrc && pcSrc.PartyId > 0 && ctx.PartyMap != null)
        {
            ctx.PartyMap.ForEachOnSameMap(pcSrc, m =>
            {
                if (m.Id.Value == target.Id.Value) return;
                _units?.Place(m, SkillIds.MG_SAFETYWALL, (ushort)(skillLevel + 5), m.X, m.Y);
                _units?.Place(m, SkillIds.AL_PNEUMA, 1, m.X, m.Y);
            }, includeSelf: false);
        }
        // Bound-elemental consumption: the rAthena ST_ELEMENTALSPIRIT2
        // state is enforced at cast-init; the actual delete fires when
        // the elemental subsystem ports (out of scope for this layer).
    }
}
