using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Hand party leadership to another member (leader only). rAthena <c>clif_parse_PartyChangeLeader</c>
/// → <c>party_changeleader</c> (party.cpp). Gates the caller being the current leader, resolves the
/// new leader's char id from the cache (by account id), and drives <see cref="IIntifService.ChangePartyLeader"/>.
/// </summary>
[PacketHandler(PacketHeader.CZ_PARTY_CHANGE_LEADER)]
public class PartyChangeLeaderHandler(
    IEntityRegistry registry,
    IPartyService partyService,
    IIntifService intif,
    ILogger<PartyChangeLeaderHandler> logger
) : IPacketHandler<MapSessionData, CZ_PARTY_CHANGE_LEADER>
{
    public Task HandleAsync(MapSessionData session, CZ_PARTY_CHANGE_LEADER packet)
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
        if (member == null) return Task.CompletedTask;

        intif.ChangePartyLeader(pc.PartyId, packet.AccountId, member.CharacterId);
        logger.LogInformation("ChangePartyLeader: party {Party} → char {New}", pc.PartyId, member.CharacterId);
        return Task.CompletedTask;
    }
}
