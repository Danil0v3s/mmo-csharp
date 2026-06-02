using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Skills;

namespace Map.Server.Handlers;

/// <summary>
/// "Answer to the Warp/Teleport destination chooser" — rAthena
/// <c>clif_parse_UseSkillMap</c> (clif.cpp:13131). The player has picked a
/// map from <see cref="Core.Server.Packets.Out.ZC.ZC_WARPLIST"/>; route the
/// pick into <see cref="ISkillCastEndService.CastEndMap"/> (skill_castend_map),
/// which performs the actual Teleport (AL_TELEPORT) or Warp (AL_WARP) warp.
///
/// <para>SP and the after-cast delay were already applied by the normal cast
/// flow when the skill was cast (StartCast deducts SP + sets the lock); this
/// handler is purely the destination-selection half. The rAthena deferred-
/// consume (SKILL_NOCONSUME_REQ) + cancel-refund + the real Warp Portal
/// ground-unit placement are tracked in COMBAT-67.</para>
/// </summary>
[PacketHandler(PacketHeader.CZ_SELECT_WARPPOINT)]
public class SelectWarpPointHandler(
    IEntityRegistry registry,
    ISkillCastEndService castEnd,
    ILogger<SelectWarpPointHandler> logger
) : IPacketHandler<MapSessionData, CZ_SELECT_WARPPOINT>
{
    public Task HandleAsync(MapSessionData session, CZ_SELECT_WARPPOINT packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var warped = castEnd.CastEndMap(player, packet.MapName, packet.SkillId);
        if (!warped)
        {
            logger.LogDebug(
                "CastEndMap refused: char {Char} skill {Skill} -> map {Map}",
                player.CharacterId, packet.SkillId, packet.MapName);
        }
        return Task.CompletedTask;
    }
}
