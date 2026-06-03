using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Invite a player to your party by name. rAthena <c>clif_parse_PartyInvite2</c> → <c>party_invite</c>
/// (party.cpp:498). Gates the inviter being in a party, resolves the named target online
/// (<c>map_nick2sd</c>), and sends the invite popup via
/// <see cref="IPartyClientService.NotifyJoinRequest"/> (which stashes the pending invite the reply
/// handler later consumes).
/// </summary>
[PacketHandler(PacketHeader.CZ_PARTY_JOIN_REQ)]
public class PartyInviteHandler(
    IEntityRegistry registry,
    IPartyService partyService,
    IPartyClientService partyClient,
    ILogger<PartyInviteHandler> logger
) : IPacketHandler<MapSessionData, CZ_PARTY_JOIN_REQ>
{
    public Task HandleAsync(MapSessionData session, CZ_PARTY_JOIN_REQ packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity inviter)
        {
            return Task.CompletedTask;
        }

        // rAthena party_invite: the inviter must already be in a party (only members invite).
        if (inviter.PartyId == 0)
        {
            logger.LogDebug("PartyInvite: char {Char} is not in a party", inviter.CharacterId);
            return Task.CompletedTask;
        }
        if (string.IsNullOrWhiteSpace(packet.TargetName)) return Task.CompletedTask;

        // map_nick2sd — resolve the named character online (this map server).
        var target = registry.All().OfType<PlayerEntity>()
            .FirstOrDefault(p => string.Equals(p.Name, packet.TargetName, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            logger.LogDebug("PartyInvite: target '{Name}' not found online", packet.TargetName);
            return Task.CompletedTask;
        }
        if (target.CharacterId == inviter.CharacterId) return Task.CompletedTask; // not self
        if (target.PartyId != 0)
        {
            logger.LogDebug("PartyInvite: target {Char} already in party {Party}", target.CharacterId, target.PartyId);
            return Task.CompletedTask;
        }

        var partyName = partyService.Get(inviter.PartyId)?.Name ?? string.Empty;
        partyClient.NotifyJoinRequest(target, inviter, inviter.PartyId, partyName);
        logger.LogInformation("PartyInvite: char {Char} invited {Target} to party {Party}",
            inviter.CharacterId, target.CharacterId, inviter.PartyId);
        return Task.CompletedTask;
    }
}
