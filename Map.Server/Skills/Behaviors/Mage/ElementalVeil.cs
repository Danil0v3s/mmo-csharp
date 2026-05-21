using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// EM_ELEMENTAL_VEIL — Elemental Master Elemental Veil. Manual port of
/// <c>rathena-fork/src/map/skills/mage/elementalveil.cpp</c>.
///
/// <para>Buffs the caster's bound EM-tier elemental with SC_ELEMENTAL_VEIL.
/// Bound-elemental binding + the SC application on a non-Entity
/// elemental target is TODO until that surface lands; we broadcast the
/// no-damage frame and fail-fast if the caster isn't a player.</para>
/// </summary>
public sealed class ElementalVeil : SkillImpl
{
    public ElementalVeil() : base(SkillIds.EM_ELEMENTAL_VEIL) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (src is not PlayerEntity sd) return;
        // TODO: read sd.BoundElemental, verify it's an EM-tier elemental, then sc_start(SC_ELEMENTAL_VEIL).
        ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SummonNone);
    }
}
