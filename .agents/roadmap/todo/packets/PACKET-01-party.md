# PACKET-01-party — Party client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-party (PartyService / PartyClientService / Intif party RPCs already exist) · **Blocks:** none

## Problem

The party service layer is fully implemented (`Map.Server/Party/PartyService.cs`,
`PartyClientService.cs`, `PartyShareService.cs`) and char-side persistence RPCs are wired
through `IIntifService` (`CreateParty`, `AddPartyMember`, `ChangePartyLeader`,
`PartyChangeOption`, `LeaveParty`, `BreakParty`). But the **only** party packets a client
can send today are `CZ_PARTY_JOIN_REQ_ACK` (reply to invite) and `CZ_REQUEST_CHAT_PARTY`
(party chat). A player therefore cannot **invite**, **leave**, **expel**, **change leader**,
or **toggle exp/item share** from the client — those features are dead-ended in the service
layer with no packet to trigger them.

## Current state (C#)

- `Map.Server/Handlers/Party/PartyJoinReqAckHandler.cs` — handles `CZ_PARTY_JOIN_REQ_ACK`
  (accept/refuse), the only party-membership packet wired. Drives `IIntifService.AddPartyMember`
  + `IPartyClientService.NotifyInviteReply`.
- `Map.Server/Handlers/Chat/PartyChatHandler.cs` — handles `CZ_REQUEST_CHAT_PARTY`.
- `Map.Server/Party/IPartyClientService.cs` — has `NotifyJoinRequest`, `NotifyInviteReply`,
  `NotifyMemberJoined`, `NotifyMemberWithdraw`, `NotifyOptionChanged`, `StashPendingInvite`,
  `ConsumePendingInvite`. **No handler calls `StashPendingInvite` yet** because there is no
  invite-request packet.
- `Map.Server/Services/Intif/IIntifService.cs:39-47` — `CreateParty`, `RequestPartyInfo`,
  `AddPartyMember`, `ChangePartyLeader`, `PartyChangeOption`, `LeaveParty`, `BreakParty`,
  `PartyMessage`.
- Packet ids registered today: `CZ_PARTY_JOIN_REQ_ACK = 0x02c7`, `CZ_REQUEST_CHAT_PARTY = 0x0108`,
  `ZC_NOTIFY_CHAT_PARTY = 0x0109`, `ZC_NOTIFY_HP_TO_GROUPM = 0x0106` (see `Core.Server/Packets/PacketHeader.cs`).

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions (the client→map entry points to port):

- `clif_parse_PartyInvite2` (modern, name-based invite) and `clif_parse_PartyInvite`
  (legacy, AID-based) → `party_invite` (`party.cpp`). Emits `ZC_PARTY_JOIN_REQ` on the target.
- `clif_parse_ReplyPartyInvite2` / `clif_parse_ReplyPartyInvite` → `party_reply_invite`
  (already handled C#-side).
- `clif_parse_LeaveParty` (`clif.cpp:13889`) → `party_leave` → `intif_party_leave`.
- `clif_parse_RemovePartyMember` (`clif.cpp:13902`, expel) → `party_removemember` (leader only).
- `clif_parse_PartyChangeLeader` → `party_changeleader`.
- `clif_parse_PartyChangeOption` (`clif.cpp:13916`) → `party_setoption` — exp-share +
  item-pickup/share rules. This is the `CZ_REQ_GROUPINFO_CHANGE` / `CZ_CHANGE_GROUPEXPOPTION`
  pair (the latter carries the item rule byte on newer clients).

ZC responses: `ZC_PARTY_JOIN_REQ` (invite popup), `ZC_ADD_MEMBER_TO_GROUP` (member added —
already fanned out via `NotifyMemberJoined` IPC completion), `ZC_DELETE_MEMBER_FROM_GROUP`
(`NotifyMemberWithdraw`), `ZC_REQ_GROUPINFO_CHANGE_V2` / `ZC_GROUPINFO_CHANGE` (option ack
via `NotifyOptionChanged`), `ZC_NOTIFY_HP_TO_GROUPM` (party HP bar, `ZC_GROUP_HP`).

**Read `clif_packetdb.hpp` for the numeric ids** — they are PACKETVER-shuffled; do not hardcode
without confirming against the project's target PACKETVER. Do not fabricate.

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_PARTY_JOIN_REQ` (invite request) — `clif_parse_PartyInvite2`: `<char_name>.24B`
        (name-based). Add the legacy AID variant only if target PACKETVER needs it.
  - [ ] `CZ_REQ_LEAVE_GROUP` (`clif_parse_LeaveParty`) — header-only or `<account_id>.L` per id.
  - [ ] `CZ_REQ_EXPEL_GROUP_MEMBER` (`clif_parse_RemovePartyMember`) — `<account_id>.L <char_name>.24B`.
  - [ ] `CZ_CHANGE_GROUP_MASTER` (`clif_parse_PartyChangeLeader`) — `<account_id>.L`.
  - [ ] `CZ_REQ_GROUPINFO_CHANGE` / `CZ_CHANGE_GROUPEXPOPTION` (`clif_parse_PartyChangeOption`)
        — `<exp_option>.L` (+ `<item_pickup>.B <item_share>.B` on the V2 variant).
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`):
  - [ ] `ZC_PARTY_JOIN_REQ` — invite popup: `<party_id>.L <party_name>.24B`.
  - [ ] `ZC_GROUPINFO_CHANGE` / `ZC_REQ_GROUPINFO_CHANGE_V2` — option ack.
  - [ ] `ZC_GROUP_HP` / `ZC_NOTIFY_HP_TO_GROUPM` (id `0x0106` already in `PacketHeader`) — HP-bar
        update broadcast to party members. Confirm the V2 (`ZC_NOTIFY_HP_TO_GROUPM_R2`) shape.
- [ ] **PacketHeader.cs**: add the missing enum entries with their confirmed ids.
- [ ] **appsettings.packets.json**: register the variable-length entries (name-bearing packets)
      under `PacketVersions`.
- [ ] **Handlers** (`Map.Server/Handlers/Party/`):
  - [ ] `PartyInviteRequestHandler` — resolve target `PlayerEntity` by name via `IEntityRegistry`,
        call `IPartyClientService.StashPendingInvite` + `NotifyJoinRequest` (emits `ZC_PARTY_JOIN_REQ`).
        Gate: inviter must be party leader (`IPartyService.IsLeader`), target must be partyless.
  - [ ] `PartyLeaveHandler` — `IIntifService.LeaveParty(partyId, accountId, charId)`; withdraw
        fan-out lands on IPC completion via `NotifyMemberWithdraw`.
  - [ ] `PartyExpelHandler` — leader-gate, then `IIntifService.LeaveParty` with the expel reason byte.
  - [ ] `PartyChangeLeaderHandler` — `IIntifService.ChangePartyLeader`.
  - [ ] `PartyOptionHandler` — `IIntifService.PartyChangeOption(partyId, accountId, exp, item, flag)`;
        ack via `NotifyOptionChanged` on completion.
- [ ] **Party HP bar**: emit `ZC_GROUP_HP` from the HP-update path so members see each other's bars.
      Wire from the existing HP-change observer to a fan-out over `MapPartyEntity` members.
- [ ] No new char-side RPC — all persistence RPCs already exist in `IIntifService`.

## Done criteria

- A player who is party leader can invite a partyless player by name; the target receives the
  popup and the accept path produces a `ZC_ADD_MEMBER_TO_GROUP` for all members.
- Leave / expel produce `ZC_DELETE_MEMBER_FROM_GROUP` with the correct reason byte (0=leave,
  1=expel) matching rAthena `party.cpp`.
- Change-leader updates the leader flag for all members; non-leaders attempting expel/leader-change/
  invite are silently rejected (matches rAthena leader gate).
- Toggling exp/item share emits the option-change ack and the rule is persisted via char side.
- Party HP bars update for all online members.
- No `// TODO`, no log-only no-op in any touched handler.

## Test plan

- `Map.Server.Tests` (or `Char.Server.Tests` parity): handler tests pinning the leader-gate
  rejection for invite/expel/leader-change, and that each handler calls the matching
  `IIntifService` method with the exact args.
- Pin the leave-vs-expel reason byte (0 vs 1).
- Manual: two clients, invite → accept → both see member list; toggle exp share; expel.

## Notes / gotchas

- Invite is name-based on modern clients (`PartyInvite2`); resolve the name to an online
  `PlayerEntity` — if the target is on another map server the invite path differs (out of scope
  here unless cross-server invite is already supported by `PartyClientService`).
- The accept/withdraw broadcasts arrive on the **IPC completion** path, not synchronously in the
  handler — do not double-emit `ZC_ADD_MEMBER_TO_GROUP` from the handler.
- `CZ_PARTY_JOIN_REQ_ACK` is already done; do not touch `PartyJoinReqAckHandler`.
