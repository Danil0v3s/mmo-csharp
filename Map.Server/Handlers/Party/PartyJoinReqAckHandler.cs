using Core.Server.Network;
using Core.Server.Packets;
using Core.Server.Packets.In.CZ;
using Map.Server.Entities;
using Map.Server.Party;
using Map.Server.Services.Intif;
using Map.Server.Session;

namespace Map.Server.Handlers.Party;

/// <summary>
/// Client replies to a party invite popup. rAthena
/// <c>clif_parse_ReplyPartyInvite</c> (clif.cpp:11860) →
/// <c>party_reply_invite</c> (party.cpp:559). The invited player sent
/// <c>CZ_PARTY_JOIN_REQ_ACK</c> with <c>flag = 0 (refuse)</c> or
/// <c>flag = 1 (accept)</c>; this handler resolves the pending invite
/// stashed by <see cref="IPartyClientService.NotifyJoinRequest"/>,
/// dispatches the char-side membership update via
/// <see cref="IIntifService.AddPartyMember"/> on accept, and notifies
/// the inviter via
/// <see cref="IPartyClientService.NotifyInviteReply"/> on refuse.
/// </summary>
[PacketHandler(PacketHeader.CZ_PARTY_JOIN_REQ_ACK)]
public class PartyJoinReqAckHandler(
    IEntityRegistry registry,
    IPartyClientService partyClient,
    IIntifService intif,
    ILogger<PartyJoinReqAckHandler> logger
) : IPacketHandler<MapSessionData, CZ_PARTY_JOIN_REQ_ACK>
{
    public Task HandleAsync(MapSessionData session, CZ_PARTY_JOIN_REQ_ACK packet)
    {
        if (session.AuthState != MapAuthState.Spawned
            || session.EntityId is not { } eid
            || registry.Get(eid) is not PlayerEntity target)
        {
            return Task.CompletedTask;
        }

        // rAthena party_reply_invite (party.cpp:566) — gate: refuse the
        // ack if the target has no pending invite for this party (client
        // sent a stale reply or never received the popup).
        var pending = partyClient.ConsumePendingInvite(target.CharacterId);
        if (pending == null)
        {
            logger.LogDebug(
                "CZ_PARTY_JOIN_REQ_ACK: char {Char} has no pending invite for party {Party}",
                target.CharacterId, packet.PartyId);
            return Task.CompletedTask;
        }
        if (pending.Value.partyId != (int)packet.PartyId)
        {
            logger.LogDebug(
                "CZ_PARTY_JOIN_REQ_ACK: char {Char} replied to party {Party} but pending invite was for {Pending}",
                target.CharacterId, packet.PartyId, pending.Value.partyId);
            // Drop the reply — but keep the pending slot consumed (we already drained it).
            return Task.CompletedTask;
        }

        // Resolve the inviter — they may have moved to another map server
        // since the popup landed. If we can't see them locally, the
        // accept path still works (char side handles the add); only the
        // refuse-path inviter notify is lossy.
        var inviter = registry.Get(new EntityId(pending.Value.inviterCharacterId)) as PlayerEntity;

        if (packet.Flag == 0)
        {
            // Refuse — rAthena clif_party_invite_reply with PARTY_REPLY_REJECTED (1).
            if (inviter != null)
            {
                partyClient.NotifyInviteReply(inviter, target.Name ?? string.Empty, result: 1);
            }
            logger.LogInformation(
                "Party invite rejected: target {Target} party {Party} inviter {Inviter}",
                target.CharacterId, packet.PartyId, pending.Value.inviterCharacterId);
            return Task.CompletedTask;
        }

        // Accept — rAthena party_reply_invite (party.cpp:584) →
        // intif_party_addmember. The char-side response broadcast lands
        // via NotifyMemberJoined when the PartyAddMember IPC finishes.
        intif.AddPartyMember(pending.Value.partyId, target);

        // Echo the "accepted" reply back to the inviter so their UI
        // shows the success popup (MsgStringTable[82]). The actual
        // ZC_ADD_MEMBER_TO_GROUP fan-out lands on the IPC completion
        // path.
        if (inviter != null)
        {
            partyClient.NotifyInviteReply(inviter, target.Name ?? string.Empty, result: 2);
        }
        logger.LogInformation(
            "Party invite accepted: target {Target} party {Party} inviter {Inviter}",
            target.CharacterId, packet.PartyId, pending.Value.inviterCharacterId);
        return Task.CompletedTask;
    }
}
