using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_ELEMENTAL_SHIELD — Sorcerer Elemental Shield. Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalshield.cpp</c>.
///
/// <para>Drops Safety Wall (lv+5) and Pneuma (lv 1) on the target's
/// tile. Consumes the bound elemental (rAthena calls
/// <c>elemental_delete</c> when the skill state is
/// <c>ST_ELEMENTALSPIRIT2</c>). Party-wide splash for parties of more
/// than one player is TODO until <c>party_foreachsamemap</c> lands.</para>
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
        // TODO: party splash + bound-elemental consumption.
    }
}
