using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Leave your party. rAthena <c>clif_parse_LeaveParty</c> → <c>party_leave</c> (party.cpp). Drives the
/// char-side leave via <see cref="IIntifService.LeaveParty"/> (which fans out the withdraw broadcast
/// on success) and clears the leaver's party id locally.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_LEAVE_GROUP)]
public class PartyLeaveHandler(
    IEntityRegistry registry,
    IIntifService intif,
    ILogger<PartyLeaveHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_LEAVE_GROUP>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_LEAVE_GROUP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }
        if (pc.PartyId == 0) return Task.CompletedTask;

        var partyId = pc.PartyId;
        intif.LeaveParty(partyId, pc.AccountId, pc.CharacterId);
        pc.PartyId = 0; // the player left — solo again
        logger.LogInformation("LeaveParty: char {Char} left party {Party}", pc.CharacterId, partyId);
        return Task.CompletedTask;
    }
}
