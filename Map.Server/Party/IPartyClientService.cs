using Map.Server.Entities;

namespace Map.Server.Party;

/// <summary>
/// Wire-emit layer for party-lifecycle packets. Distinct from
/// <see cref="IPartyService"/> (the cache + helper family) and from
/// <see cref="Services.Intif.IIntifService"/> (the gRPC dispatch
/// surface).
/// <para>This service is the C# analog of the rAthena <c>clif_party_*</c>
/// emitter family — every method maps to a specific
/// <c>clif_party_*</c> function and ends in a
/// <c>session.EnqueuePacket(new ZC_*)</c> call.</para>
/// </summary>
public interface IPartyClientService
{
    /// <summary>
    /// rAthena <c>clif_party_created</c> (clif.cpp:7792) +
    /// <c>clif_party_member_info</c> (clif.cpp:7812). Sent after a
    /// successful char-side <c>PartyCreate</c> response: ack the
    /// founder with the result byte, then broadcast the founding
    /// member entry (which the client uses to populate the party
    /// window with the leader row).
    /// </summary>
    void NotifyPartyCreated(PlayerEntity founder, int partyId, string partyName);

    /// <summary>
    /// rAthena <c>clif_party_invite</c> (clif.cpp:7927). Pushes the
    /// invite popup to <paramref name="target"/>. The inviter's
    /// session id + party id is stashed in a per-target pending-invite
    /// slot; <see cref="ConsumePendingInvite"/> drains it when the
    /// target replies via <c>CZ_PARTY_JOIN_REQ_ACK</c>.
    /// </summary>
    void NotifyJoinRequest(PlayerEntity target, PlayerEntity inviter, int partyId, string partyName);

    /// <summary>
    /// rAthena <c>clif_party_invite_reply</c> (clif.cpp:7958). Sent to
    /// the inviter after the invitee accepts / rejects / times out.
    /// Result codes follow the <c>e_party_invite_reply</c> enum
    /// (clif.cpp:7947-7957).
    /// </summary>
    void NotifyInviteReply(PlayerEntity inviter, string targetName, int result);

    /// <summary>
    /// rAthena <c>clif_party_member_info</c> (clif.cpp:7812). Broadcast
    /// the new-member entry to every locally-loaded party member
    /// (PARTY scope). Used when a char joins via accepted-invite or
    /// /joinparty.
    /// </summary>
    void NotifyMemberJoined(MapPartyEntity party, MapPartyMember newMember);

    /// <summary>
    /// rAthena <c>clif_party_withdraw</c> (clif.cpp:8025). Broadcast
    /// the leaving / expelled member entry to every locally-loaded
    /// party member. Also removes the member from the cached
    /// <see cref="MapPartyEntity.Members"/> dictionary so subsequent
    /// reads see the post-withdraw state.
    /// </summary>
    /// <param name="reason">0 = left, 1 = expelled. rAthena
    /// <c>e_party_member_withdraw</c>.</param>
    void NotifyMemberWithdraw(MapPartyEntity party, int accountId, string memberName, byte reason);

    /// <summary>
    /// rAthena <c>clif_party_option</c> (clif.cpp:7987). Broadcast the
    /// new EXP / item-pickup / item-share flags to every locally-loaded
    /// party member after a <c>party_changeoption</c> response. Also
    /// mutates the cached <see cref="MapPartyEntity.Exp"/> /
    /// <see cref="MapPartyEntity.Item"/> so the cache stays in sync.
    /// </summary>
    void NotifyOptionChanged(MapPartyEntity party, uint expOption, byte itemPickupRule, byte itemShareRule);

    /// <summary>
    /// rAthena <c>clif_party_xy_remove</c> (clif.cpp:10153). Sends
    /// xy = -1 / -1 to every locally-loaded party member on the same
    /// map so their minimap dot for the leaving member clears. Used on
    /// logout and on party-leave (covers <c>party_send_xy_clear</c> +
    /// <c>party_send_dot_remove</c>).
    /// </summary>
    void NotifyDotRemove(MapPartyEntity party, int characterId, int accountId);

    /// <summary>
    /// Stash a pending invite so the target's
    /// <c>CZ_PARTY_JOIN_REQ_ACK</c> can resolve the inviter and party.
    /// Already called by <see cref="NotifyJoinRequest"/>; exposed here
    /// for the GM / atcommand path (<c>/invite</c>) that may push the
    /// popup outside the normal flow.
    /// </summary>
    void StashPendingInvite(int targetCharacterId, int partyId, int inviterCharacterId);

    /// <summary>
    /// Drain the pending invite for <paramref name="targetCharacterId"/>.
    /// Returns null when no invite is outstanding (client replied to a
    /// stale popup). Otherwise yields <c>(partyId, inviterCharacterId)</c>
    /// for the <c>CZ_PARTY_JOIN_REQ_ACK</c> handler.
    /// </summary>
    (int partyId, int inviterCharacterId)? ConsumePendingInvite(int targetCharacterId);
}
