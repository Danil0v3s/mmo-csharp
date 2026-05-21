using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_ANCILLA — Arch Bishop Ancilla. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ancilla.cpp</c>.
///
/// <para>Produces one Ancilla item (an SP-restoring consumable)
/// using <c>skill_produce_mix</c> on the caster. Player-only —
/// non-PC casters skip silently.</para>
///
/// <para>The C# port doesn't yet have <c>ISkillProductionService.Produce</c>
/// wired for ad-hoc skill-produced items at this entry point; the
/// cast frame is broadcast faithfully and the production call is
/// marked TODO. Skill-driven item production lands separately
/// (SK-M4 in the parity roadmap).</para>
/// </summary>
public sealed class Ancilla : SkillImpl
{
    public Ancilla() : base(SkillIds.AB_ANCILLA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: if (sd) { clif_skill_nodamage(...); skill_produce_mix(sd, AB_ANCILLA, ITEMID_ANCILLA, 0,0,0, 1, -1); }
        if (src is not PlayerEntity) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // TODO: ISkillProductionService.Produce(sd, ITEMID_ANCILLA, 1).
        // The fork's production helper handles the SP-cost + cooldown +
        // inventory-overweight check. Until wired through, no item is
        // actually granted — broadcast lands so the cast visual plays.
    }
}
