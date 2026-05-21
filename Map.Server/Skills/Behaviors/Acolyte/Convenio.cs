using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_CONVENIO — Arch Bishop Convenio. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/convenio.cpp</c>.
///
/// <para>Party-only teleport-to-caster. Requires the caster to be
/// the party leader; pulls every party member (same map, alive,
/// /call enabled) to the caster's cell. Map-flag gates: NoTeleport,
/// PvP, Battleground, and GvG maps reject the cast entirely.</para>
///
/// <para>Cross-map / party-iteration infrastructure isn't surfaced
/// yet — this port is structural (the party / map-flag guards are
/// stubs). Full implementation lands once
/// <c>IPartyMapService.ForEachOnSameMap</c> + <c>IMapFlagService</c>
/// route through SkillBehaviorContext.</para>
/// </summary>
public sealed class Convenio : SkillImpl
{
    public Convenio() : base(SkillIds.AB_CONVENIO) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity caster) return;

        // rAthena: requires party + leader. Currently we can't read
        // party-leader flag from the map side (party state lives on
        // char-server). Emit fail-broadcast until the IPC surfaces it.
        ctx.Client?.BroadcastSkillFail(caster, SkillId,
            Core.Server.Packets.Out.ZC.SkillFailCause.NeedHelpers);

        // TODO: when party-leader + party-member iteration is wired:
        //   1. Verify caster is party leader.
        //   2. For each party member on the same map (excluding caster, dead, disabled-call):
        //        if (map allows teleport) pc_setpos(member, src.MapId, src.X, src.Y, CLR_TELEPORT)
        //   3. If 0 teleported, emit clif_skill_fail.
    }
}
