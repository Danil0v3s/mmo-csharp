using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_ANCILLA — Arch Bishop Ancilla. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/ancilla.cpp</c>.
///
/// <para>Produces one Ancilla item (rAthena <c>ITEMID_ANCILLA = 12333</c>),
/// an SP-restoring consumable, into the caster's bag. Player-only —
/// non-PC casters skip silently.</para>
///
/// <para>rAthena dispatches via <c>skill_produce_mix(sd, AB_ANCILLA,
/// ITEMID_ANCILLA, 0, 0, 0, 1, -1)</c>. The C# port grants the item
/// through the session inventory bridge; the SP-cost + cooldown gates
/// already fire in the cast pipeline upstream so this hook only owns
/// the item delivery.</para>
/// </summary>
public sealed class Ancilla : SkillImpl
{
    public Ancilla() : base(SkillIds.AB_ANCILLA) { }

    /// <summary>rAthena <c>ITEMID_ANCILLA</c> — id 12333.</summary>
    private const uint ItemIdAncilla = 12333;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);

        // rAthena: skill_produce_mix(sd, AB_ANCILLA, ITEMID_ANCILLA, ...).
        // Grant 1 Ancilla to the caster's inventory.
        if (ctx.Sessions != null && ctx.Inventory != null)
        {
            var session = ctx.Sessions.TryGet(caster);
            if (session != null)
            {
                ctx.Inventory.GiveItem(session, ItemIdAncilla, 1);
            }
        }
    }
}
