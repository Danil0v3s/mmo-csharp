using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Services;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Create a party. rAthena <c>clif_parse_CreateParty</c> → <c>party_create</c> (party.cpp). Gates the
/// founder not already being in a party, drives the char-side <c>PartyCreate</c> RPC, and on success
/// stamps the founder's party id + notifies the client (<see cref="IPartyClientService.NotifyPartyCreated"/>
/// emits the create ack + the leader's member row).
/// </summary>
[PacketHandler(PacketHeader.CZ_MAKE_GROUP)]
public class PartyCreateHandler(
    IEntityRegistry registry,
    ICharServerIpcServiceParty partyIpc,
    IPartyClientService partyClient,
    ILogger<PartyCreateHandler> logger
) : IPacketHandler<MapSessionData, CZ_MAKE_GROUP>
{
    public async Task HandleAsync(MapSessionData session, CZ_MAKE_GROUP packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity founder)
        {
            return;
        }

        // rAthena party_create gate: a player already in a party can't create one.
        if (founder.PartyId != 0)
        {
            logger.LogDebug("CreateParty: char {Char} already in party {Party}", founder.CharacterId, founder.PartyId);
            return;
        }
        if (string.IsNullOrWhiteSpace(packet.PartyName)) return;

        var resp = await partyIpc.PartyCreateAsync(
            name: packet.PartyName, item: 0, item2: 0,
            leaderAccountId: founder.AccountId, leaderCharacterId: founder.CharacterId,
            leaderName: founder.Name, leaderClassId: founder.ClassId,
            leaderMapName: session.CharacterData?.MapName ?? string.Empty, leaderLevel: (uint)founder.Level);

        if (resp is not { Success: true } || resp.PartyId <= 0)
        {
            logger.LogInformation("CreateParty: char {Char} '{Name}' refused ({Err})",
                founder.CharacterId, packet.PartyName, resp?.ErrorMessage);
            return;
        }

        founder.PartyId = resp.PartyId;
        partyClient.NotifyPartyCreated(founder, resp.PartyId, packet.PartyName);
        logger.LogInformation("CreateParty: char {Char} created party {Party} '{Name}'",
            founder.CharacterId, resp.PartyId, packet.PartyName);
    }
}
