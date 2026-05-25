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
        if (caster.PartyId <= 0 || ctx.PartyMap == null || ctx.Setpos == null)
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.NeedHelpers);
            return;
        }

        // Wave 87 — party_isleader gate now wired through IPartyService.
        // Conservatively allow the cast when the cache is empty (hydrate-
        // on-demand may not have populated for a freshly-logged-in caster);
        // strict-fail when the cache shows a party and the caster isn't
        // the leader.
        if (ctx.Party != null && ctx.Party.Get(caster.PartyId) != null && !ctx.Party.IsLeader(caster))
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.NeedHelpers);
            return;
        }

        // rAthena AB_CONVENIO: walk every same-map party member and
        // warp them to the caster's cell.
        var teleported = 0;
        var dstX = caster.X;
        var dstY = caster.Y;
        // Resolve the caster's current map name from the hashed MapId.
        string? mapName = null;
        if (ctx.World != null)
        {
            foreach (var m in ctx.World.All)
            {
                if ((uint)m.Name.GetHashCode() == caster.MapId) { mapName = m.Name; break; }
            }
        }
        if (mapName == null)
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.NeedHelpers);
            return;
        }
        ctx.PartyMap.ForEachOnSameMap(caster, m =>
        {
            if (m.Id.Value == caster.Id.Value) return;
            if (m is not PlayerEntity pcm) return;
            var result = ctx.Setpos.Setpos(pcm, mapName, dstX, dstY);
            if (result == Map.Server.Movement.SetposResult.Ok) teleported++;
        }, includeSelf: false);

        if (teleported == 0)
        {
            ctx.Client?.BroadcastSkillFail(caster, SkillId,
                Core.Server.Packets.Out.ZC.SkillFailCause.NeedHelpers);
        }
        else
        {
            ctx.Client?.BroadcastSkillNoDamage(src, caster, SkillId, skillLevel);
        }
    }
}
