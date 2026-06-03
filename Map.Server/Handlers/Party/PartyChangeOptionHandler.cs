using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Change the party EXP-share option (leader only). rAthena <c>clif_parse_PartyChangeOption</c> →
/// <c>party_changeoption</c> (party.cpp). Gates the caller being the leader, keeps the existing
/// item-share policy (rAthena passes <c>p->party.item</c>), and drives
/// <see cref="IIntifService.PartyChangeOption"/> (which fans out the option-change broadcast).
/// </summary>
[PacketHandler(PacketHeader.CZ_PARTY_CHANGE_OPTION)]
public class PartyChangeOptionHandler(
    IEntityRegistry registry,
    IPartyService partyService,
    IIntifService intif,
    ILogger<PartyChangeOptionHandler> logger
) : IPacketHandler<MapSessionData, CZ_PARTY_CHANGE_OPTION>
{
    public Task HandleAsync(MapSessionData session, CZ_PARTY_CHANGE_OPTION packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity pc)
        {
            return Task.CompletedTask;
        }
        if (pc.PartyId == 0 || !partyService.IsLeader(pc)) return Task.CompletedTask;

        var currentItem = partyService.Get(pc.PartyId)?.Item ?? 0;
        intif.PartyChangeOption(pc.PartyId, pc.AccountId, packet.ExpFlag, currentItem, flag: 0);
        logger.LogInformation("ChangePartyOption: party {Party} exp={Exp}", pc.PartyId, packet.ExpFlag);
        return Task.CompletedTask;
    }
}
