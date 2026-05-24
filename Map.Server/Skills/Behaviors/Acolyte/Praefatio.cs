using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_PRAEFATIO — Arch Bishop Praefatio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/praefatio.cpp</c>.
///
/// <para>Party-wide Praefatio buff (damage-absorb shield similar to
/// Kyrie Eleison but scales with party size — Val4 carries the
/// party count for the SC handler to read).</para>
///
/// <para>The SC type <c>SC_PRAEFATIO</c> isn't yet on our StatusType
/// enum — once it lands, the apply call will use the proper SC.
/// For now we broadcast the cast frame and TODO the SC.</para>
/// </summary>
public sealed class Praefatio : SkillImpl
{
    public Praefatio() : base(SkillIds.AB_PRAEFATIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: sc_start4(SC_PRAEFATIO, 100, lv, 0, 0, party_count, skill_get_time(...))
        // SC missing on our enum — broadcast the cast frame; the SC
        // application + party-count Val4 land once SC_PRAEFATIO is added.
        ctx.Client?.BroadcastSkillNoDamage(target, target, SkillId, skillLevel);

        // Deferred per PARITY-REMAINING.md §P2.3: SC_PRAEFATIO not yet on the
        // StatusType enum. Once added, apply with Val4 = party member count
        // for proportional damage absorb.
    }
}
