using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Achievement;
using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Handlers.Achievement;

/// <summary>
/// Client equips an achievement title shown over the character name. rAthena
/// <c>clif_parse_change_title</c> ([clif.cpp:20721], 0x0a2e). The service validates ownership
/// (a positive title id must come from a rewarded achievement), stores the equipped id, re-broadcasts
/// the name block to viewers, and acks with <c>ZC_ACK_CHANGE_TITLE</c> (result 0 = applied,
/// 1 = not owned). A title id ≤ 0 clears the title; an unchanged id is a silent no-op.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_CHANGE_TITLE)]
public class ChangeTitleHandler(
    IEntityRegistry registry,
    IAchievementService achievements,
    ILogger<ChangeTitleHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_CHANGE_TITLE>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_CHANGE_TITLE packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }

        if (achievements.SetTitle(pc, packet.TitleId))
            logger.LogInformation("CZ_REQ_CHANGE_TITLE: char {Char} title -> {Title}",
                pc.CharacterId, packet.TitleId);

        return Task.CompletedTask;
    }
}
