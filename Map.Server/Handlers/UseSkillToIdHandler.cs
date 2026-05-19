using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Skills;

namespace Map.Server.Handlers;

/// <summary>
/// "Use skill on target" — rAthena <c>clif_parse_UseSkillToId</c>
/// (clif.cpp:12968). Routes the validated request through
/// <see cref="ISkillCastService"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_USE_SKILL_TOID)]
public class UseSkillToIdHandler(
    IEntityRegistry registry,
    ISkillCastService skillCast,
    ILogger<UseSkillToIdHandler> logger
) : IPacketHandler<MapSessionData, CZ_USE_SKILL_TOID>
{
    public Task HandleAsync(MapSessionData session, CZ_USE_SKILL_TOID packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity player)
        {
            return Task.CompletedTask;
        }

        var result = skillCast.StartCast(player, new EntityId(packet.TargetId), packet.SkillId, packet.SkillLevel);
        if (result != SkillCastResult.Started)
        {
            logger.LogDebug(
                "Skill cast rejected: char {Char} skill {Skill}@{Lvl} -> target {Target} -- {Reason}",
                player.CharacterId, packet.SkillId, packet.SkillLevel, packet.TargetId, result);
        }
        return Task.CompletedTask;
    }
}
