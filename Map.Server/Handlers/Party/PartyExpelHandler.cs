using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Expel a member (leader only). rAthena <c>clif_parse_RemovePartyMember</c> → <c>party_removemember</c>
/// (party.cpp). Gates the caller being the party leader, resolves the target's char id from the party
/// cache (by account id), and drives the char-side removal via <see cref="IIntifService.LeaveParty"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_REQ_EXPEL_GROUP_MEMBER)]
public class PartyExpelHandler(
    IEntityRegistry registry,
    IPartyService partyService,
    IIntifService intif,
    ILogger<PartyExpelHandler> logger
) : IPacketHandler<MapSessionData, CZ_REQ_EXPEL_GROUP_MEMBER>
{
    public Task HandleAsync(MapSessionData session, CZ_REQ_EXPEL_GROUP_MEMBER packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }
        if (pc.PartyId == 0 || !partyService.IsLeader(pc)) return Task.CompletedTask;

        var member = partyService.Get(pc.PartyId)?.Members.Values
            .FirstOrDefault(m => m.AccountId == packet.AccountId);
        if (member == null || member.CharacterId == pc.CharacterId) return Task.CompletedTask;

        // reason 1 = PARTY_MEMBER_EXPEL: the withdraw broadcast announces "kicked", not "left".
        intif.LeaveParty(pc.PartyId, packet.AccountId, member.CharacterId, reason: 1);
        if (registry.Get(new EntityId(member.CharacterId)) is PlayerEntity target) target.PartyId = 0;
        logger.LogInformation("ExpelParty: leader {Leader} expelled char {Target} from party {Party}",
            pc.CharacterId, member.CharacterId, pc.PartyId);
        return Task.CompletedTask;
    }
}
