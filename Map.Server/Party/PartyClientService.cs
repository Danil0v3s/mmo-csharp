using System.Collections.Concurrent;
using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Party;

/// <summary>
/// Default <see cref="IPartyClientService"/>. Translates party
/// lifecycle events into the corresponding <c>ZC_*</c> wire packets
/// and routes them via <see cref="ISessionManagerAccessor"/>.
/// <para>This mirrors rAthena's <c>clif_party_*</c> emitter family —
/// one method per <c>clif_party_*</c> entry point. Mutations to the
/// cached <see cref="MapPartyEntity"/> (option flags, member removal)
/// happen here so the cache stays consistent with what the client
/// sees on the wire.</para>
/// </summary>
public sealed class PartyClientService : IPartyClientService
{
    private readonly IEntityRegistry _entities;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<PartyClientService> _logger;

    /// <summary>
    /// Pending-invite slot keyed by the invited target's character id.
    /// rAthena uses <c>sd-&gt;party_invite</c> + <c>sd-&gt;party_invite_account</c>
    /// on the per-PC session for the same purpose (party.cpp:486-558).
    /// We use a concurrent dictionary because the invite popup is
    /// issued from the inviter's handler thread but the reply lands on
    /// the target's handler thread.
    /// </summary>
    private readonly ConcurrentDictionary<int, (int PartyId, int InviterCharacterId)> _pendingInvites = new();

    public PartyClientService(
        IEntityRegistry entities,
        ISessionManagerAccessor sessions,
        ILogger<PartyClientService> logger)
    {
        _entities = entities;
        _sessions = sessions;
        _logger = logger;
    }

    public void NotifyPartyCreated(PlayerEntity founder, int partyId, string partyName)
    {
        if (founder == null) return;
        var session = _sessions.GetByEntityId(founder.Id);
        if (session == null) return;

        // rAthena clif_party_created (clif.cpp:7792) — result=0 means
        // "party successfully organized" (MsgStringTable[77]).
        session.EnqueuePacket(new ZC_ACK_MAKE_GROUP { Result = 0 });

        // rAthena: clif_party_member_info(party, sd) on the founder so
        // the client populates the party window with the leader row.
        // PARTY scope on rAthena's side; with only one member at the
        // moment of creation, SELF == PARTY.
        session.EnqueuePacket(new ZC_ADD_MEMBER_TO_GROUP
        {
            AccountId = (uint)founder.AccountId,
            CharacterId = (uint)founder.CharacterId,
            Leader = 0, // founder is leader (rAthena inverts: 0 == leader)
            ClassId = (short)founder.ClassId,
            BaseLevel = (short)founder.Level,
            X = founder.X,
            Y = founder.Y,
            Offline = 0, // founder is online
            PartyName = partyName ?? string.Empty,
            PlayerName = founder.Name ?? string.Empty,
            MapName = string.Empty, // map name lookup is best-effort; empty is acceptable
            SharePickup = 0,
            ShareLoot = 0,
        });

        _logger.LogInformation(
            "Party created: char {Char} party {PartyId} '{Name}'",
            founder.CharacterId, partyId, partyName);
    }

    public void NotifyJoinRequest(PlayerEntity target, PlayerEntity inviter, int partyId, string partyName)
    {
        if (target == null || inviter == null) return;
        var session = _sessions.GetByEntityId(target.Id);
        if (session == null)
        {
            _logger.LogDebug(
                "NotifyJoinRequest: target {Char} not on this map server",
                target.CharacterId);
            return;
        }

        StashPendingInvite(target.CharacterId, partyId, inviter.CharacterId);

        session.EnqueuePacket(new ZC_PARTY_JOIN_REQ
        {
            PartyId = (uint)partyId,
            PartyName = partyName ?? string.Empty,
        });
    }

    public void NotifyInviteReply(PlayerEntity inviter, string targetName, int result)
    {
        if (inviter == null) return;
        var session = _sessions.GetByEntityId(inviter.Id);
        if (session == null) return;

        session.EnqueuePacket(new ZC_PARTY_JOIN_REQ_ACK
        {
            CharacterName = targetName ?? string.Empty,
            Result = result,
        });
    }

    public void NotifyMemberJoined(MapPartyEntity party, MapPartyMember newMember)
    {
        if (party == null || newMember == null) return;

        // rAthena clif_party_member_info: fan-out the new row to every
        // existing party member. Mirror the PARTY scope by walking
        // locally-loaded players whose PartyId matches; cross-server
        // fan-out is handled char-side (PartyMessage broadcast).
        var packetFactory = () => new ZC_ADD_MEMBER_TO_GROUP
        {
            AccountId = (uint)newMember.AccountId,
            CharacterId = (uint)newMember.CharacterId,
            Leader = (uint)(newMember.Leader ? 0 : 1), // rAthena inverts
            ClassId = (short)newMember.ClassId,
            BaseLevel = (short)newMember.Level,
            X = 0, // position not in MapPartyMember; client refreshes via PartyXY
            Y = 0,
            Offline = (byte)(newMember.Online ? 0 : 1),
            PartyName = party.Name ?? string.Empty,
            PlayerName = newMember.Name ?? string.Empty,
            MapName = newMember.MapName ?? string.Empty,
            SharePickup = (byte)((party.Item & 1) != 0 ? 1 : 0),
            ShareLoot = (byte)((party.Item & 2) != 0 ? 1 : 0),
        };
        BroadcastToParty(party.PartyId, packetFactory);
    }

    public void NotifyMemberWithdraw(MapPartyEntity party, int accountId, string memberName, byte reason)
    {
        if (party == null) return;

        // Find the member's char id BEFORE we remove them so the
        // broadcast contains the right AID. Members keyed by char id.
        int withdrawnCharId = 0;
        foreach (var kv in party.Members)
        {
            if (kv.Value.AccountId == accountId)
            {
                withdrawnCharId = kv.Key;
                break;
            }
        }

        // Broadcast first (so the leaving member's own session also
        // receives the withdraw row before any cache eviction races),
        // then mutate the cache.
        var packetFactory = () => new ZC_DELETE_MEMBER_FROM_GROUP
        {
            AccountId = (uint)accountId,
            CharacterName = memberName ?? string.Empty,
            Result = reason,
        };
        BroadcastToParty(party.PartyId, packetFactory);

        // rAthena party_member_withdraw (party.cpp:1163) — strikes the
        // member out of party.member[]. We key by char id; remove the
        // matching entry.
        if (withdrawnCharId > 0)
        {
            party.Members.Remove(withdrawnCharId);
        }
    }

    public void NotifyOptionChanged(MapPartyEntity party, uint expOption, byte itemPickupRule, byte itemShareRule)
    {
        if (party == null) return;

        // rAthena party_optionchanged (party.cpp:912) updates the row
        // first then fans out. Match that order so cache reads during
        // the fan-out see the new flags.
        party.Exp = (byte)(expOption == 1 ? 1 : 0);
        party.Item = (byte)((itemPickupRule != 0 ? 1 : 0) | (itemShareRule != 0 ? 2 : 0));

        var packetFactory = () => new ZC_GROUPINFO_CHANGE
        {
            ExpOption = expOption,
            ItemPickupRule = itemPickupRule,
            ItemShareRule = itemShareRule,
        };
        BroadcastToParty(party.PartyId, packetFactory);
    }

    public void NotifyDotRemove(MapPartyEntity party, int characterId, int accountId)
    {
        if (party == null) return;

        // rAthena clif_party_xy_remove (clif.cpp:10153) uses scope
        // PARTY_SAMEMAP_WOS — same map, party only, without self. We
        // approximate by sending to all locally-loaded party members
        // on the same map server (visibility / range cleanup is the
        // client's job once xy = -1 / -1 lands).
        var packetFactory = () => new ZC_NOTIFY_POSITION_TO_GROUPM
        {
            AccountId = (uint)accountId,
            X = -1,
            Y = -1,
        };
        BroadcastToParty(party.PartyId, packetFactory, excludeCharacterId: characterId);
    }

    public void StashPendingInvite(int targetCharacterId, int partyId, int inviterCharacterId)
    {
        _pendingInvites[targetCharacterId] = (partyId, inviterCharacterId);
    }

    public (int partyId, int inviterCharacterId)? ConsumePendingInvite(int targetCharacterId)
    {
        if (_pendingInvites.TryRemove(targetCharacterId, out var slot))
        {
            return (slot.PartyId, slot.InviterCharacterId);
        }
        return null;
    }

    /// <summary>
    /// Fan-out helper: enqueue a fresh packet to every locally-loaded
    /// member of <paramref name="partyId"/>. Mirrors rAthena
    /// <c>clif_send(packet, len, src, PARTY)</c> — same-server fan-out
    /// only; cross-server fan-out lands via the char-side broadcast.
    /// </summary>
    private void BroadcastToParty(int partyId, Func<Core.Server.Packets.OutgoingPacket> packet,
        int excludeCharacterId = 0)
    {
        if (partyId <= 0) return;
        foreach (var entity in _entities.All())
        {
            if (entity is not PlayerEntity pc) continue;
            if (pc.PartyId != partyId) continue;
            if (excludeCharacterId != 0 && pc.CharacterId == excludeCharacterId) continue;
            var session = _sessions.GetByEntityId(pc.Id);
            session?.EnqueuePacket(packet());
        }
    }
}
