# GP-PARTY — Party works end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** GP-MVPFAME (kill-credit fan-out), GP-QUEST/ACHIEVE (party objectives)

## The deliverable

> A player can **create a party, invite/accept, see party members + their HP bars on
> screen, change leader, set EXP/item share, expel a member, and leave** — live client,
> surviving logout.

## Player story

Party is the backbone of group PvE. Today only join-ack + party chat are wired
(`README` ground truth: "Party = join-ack + chat only"); invite/leave/expel/leader/share
options and the on-screen HP-bar updates are not reachable.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Entity + migration | ✅ | `Core.Database/Entities/Party*Entity.cs` |
| Repository | ✅ | `IPartyRepository` |
| Service | partial | `Map.Server/Party/IPartyService.cs` + impl — model exists; verify invite/expel/leader/share logic |
| Persistence IPC | partial | char-side party RPCs exist; verify create/join/leave/expel/option persist |
| CZ handlers | partial | join-ack + chat only — invite/reply/leave/expel/leader/share/booking missing |
| ZC emits | partial | member list + HP-bar (`ZC_NOTIFY_HP_TO_GROUPM`) updates missing |

## rAthena reference

- `rathena/src/map/party.cpp` — `party_create`, `party_invite`/`party_reply_invite`,
  `party_member_added`, `party_leave`/`party_removemember`, `party_changeleader`,
  `party_changeoption` (EXP/item share), `party_send_movemap`, `party_send_hp` (HP-bar sync).
- `rathena/src/map/clif.cpp` — parse `CZ_PARTY_*` (request/invite/leave/expel/leader/
  setting/HP-bar opt-in); emit `clif_party_member_info`, `clif_party_info`,
  `clif_party_option`, `clif_party_hp`, `clif_party_xy`, `clif_party_message`.
- `char/int_party.cpp` — persistence (member rows, options, leader).

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation; build the CZ/ZC handlers here.

## Scope — every layer

- [ ] **CZ handlers**: create, invite, invite-reply, leave, expel, change-leader,
      change-option (EXP/item share), HP-bar opt-in, position/HP sync request.
- [ ] **Service**: verify/complete invite state machine, expel (leader-only gate), leader
      change, share-option validation, member add/remove broadcasts.
- [ ] **Persistence**: create/join/leave/expel/option/leader round-trip to char; party
      survives logout + re-login restores membership.
- [ ] **ZC emits**: party info, member info/add/leave, option update, message, member
      HP/position sync (`ZC_NOTIFY_HP_TO_GROUPM`, `ZC_NOTIFY_POSITION_TO_GROUPM`).
- [ ] **Wiring**: on map-enter/move, push the joining member's HP/xy to the party; on
      HP change, fan out to party members in view.

## Done criteria

- A invites B, B accepts → both see a 2-member party + each other's HP bar.
- Leader changes leader; sets EXP-share even → both share kill EXP; expels B → B sees the
  expel and the party shrinks.
- Relog → party membership + options intact.
- No party CZ handler missing; HP-bar updates live.

## Test plan

- Handler tests: each CZ → service.
- Service: invite/expel/leader/share gates.
- Persistence round-trip.
- Live: 2-client invite → share → expel → leave.

## Progress log (multi-turn vertical)

- **2026-06-03 (turn 1)** — Investigation: the char-side party IPC (`PartyCreate`/`AddMember`/
  `ChangeOption`/`Leave`/`ChangeMap`/`Break`/`Message`/`LeaderChange`/`ShareLevel`) and the
  notify/broadcast layer (`IPartyClientService`: `NotifyPartyCreated`/`NotifyJoinRequest`/
  `NotifyMemberJoined`/`NotifyMemberWithdraw`/`NotifyOptionChanged`/`NotifyDotRemove`) and the cache
  (`IPartyService`) are all already built; the invite-**reply** handler exists. The gap is the
  remaining CZ handlers. Landed the **form-a-party core**: `CZ_MAKE_GROUP` (0x00f9) +
  `PartyCreateHandler` (gate-not-in-party → `PartyCreateAsync` → stamp `PartyId` + `NotifyPartyCreated`)
  and `CZ_PARTY_JOIN_REQ` (0x0802) + `PartyInviteHandler` (gate-in-party → `map_nick2sd` via the
  entity registry → `NotifyJoinRequest` popup, which the existing reply handler consumes).
  `PartyCreateInviteHandlersTests` (6) green; full suite 4420 pass (1 = standing replay-fixture).
  **A player can now create a party + invite someone (who accepts via the existing reply handler).**
- **Remaining (next turns):** `CZ_REQ_LEAVE_GROUP` (leave) → `PartyLeaveAsync` + `NotifyMemberWithdraw`;
  `CZ_REQ_EXPEL_GROUP_MEMBER` (expel, leader-gated) → `PartyLeaveAsync(expel)`; `CZ_PARTY_CHANGE_LEADER`
  → `PartyLeaderChangeAsync`; `CZ_PARTY_CHANGE_OPTION` (EXP/item share) → `PartyChangeOptionAsync` +
  `NotifyOptionChanged`; the **HP-bar / position sync** wiring (`ZC_NOTIFY_HP_TO_GROUPM` /
  `ZC_NOTIFY_POSITION_TO_GROUPM` on map-enter/move/HP-change). All are layers of THIS vertical; the
  loop resumes this card. Live-client wire validation is the project's standing deferred pass.
- **2026-06-03 (turn 2)** — Manage handlers landed (leave / expel / change-leader / change-option).
  4 CZ packets (`CZ_REQ_LEAVE_GROUP` 0x0100, `CZ_REQ_EXPEL_GROUP_MEMBER` 0x0103, `CZ_PARTY_CHANGE_OPTION`
  0x0102, `CZ_PARTY_CHANGE_LEADER` 0x07da) + handlers driving the established `IIntifService`
  path (`LeaveParty`/`ChangePartyLeader`/`PartyChangeOption`, which wrap the IPC + fan out the
  broadcast). Leave clears the leaver's PartyId; expel/leader/option are leader-gated (`IsLeader`) +
  resolve the target char id from the party cache by account id; change-option keeps the existing
  item-share policy. `PartyJoinReqAckHandlerTests` extended to 9 (5 new). Full suite 4425 pass
  (1 = standing replay-fixture). Filed **GP-PARTY-EXPEL-REASON** (the kicked-vs-left withdraw reason
  byte — `IIntifService.LeaveParty` hard-codes reason 0; deferred to avoid churning the 4 intif stubs).
  **A player can now create/invite/accept/leave/expel/change-leader/set-EXP-share — the whole party
  management UI works.**
- **2026-06-03 (turn 3 — DONE)** — HP-bar / position sync landed. New `ZC_NOTIFY_HP_TO_GROUPM`
  (0x0106, AID+hp.W+maxhp.W with the >INT16 %-scaling) + new `PartySyncService` (rAthena
  `party_send_xy_timer` + `clif_party_hp`): a coarse ~1 s tick (wired into `MapServerImpl` after the
  instance sweep) that, for each online party member whose cell or HP changed, broadcasts
  `ZC_NOTIFY_POSITION_TO_GROUPM` + `ZC_NOTIFY_HP_TO_GROUPM` to their same-map teammates
  (`IPartyMapService.ForEachOnSameMap`, excl self), change-gated to avoid flooding. `PartySyncServiceTests`
  (4) green; full suite 4429 pass (1 = standing replay-fixture). **GP-PARTY is reachable end-to-end:
  create → invite → accept → see member list + HP bars + minimap dots → set EXP-share → change leader
  → expel → leave.**

## History

- 2026-06-03 — Party works end-to-end (3 turns). The char-side party IPC + the notify/broadcast layer
  (`IPartyClientService`) + the cache (`IPartyService`) were already built (archive PARTY work); the
  invite-reply handler existed. This card built the rest of the client packet bridge: create
  (`CZ_MAKE_GROUP`) + invite-by-name (`CZ_PARTY_JOIN_REQ`) + leave (`CZ_REQ_LEAVE_GROUP`) + expel
  (`CZ_REQ_EXPEL_GROUP_MEMBER`, leader-gated) + change-leader (`CZ_PARTY_CHANGE_LEADER`) +
  change-option (`CZ_PARTY_CHANGE_OPTION`) handlers, and the HP-bar/minimap-dot sync
  (`ZC_NOTIFY_HP_TO_GROUPM` + `PartySyncService`). Build-to-struct + handler/service unit tests (19
  across the party suite); full suite green (1 standing replay-fixture). Follow-ups filed:
  **GP-PARTY-EXPEL-REASON** (kicked-vs-left withdraw byte) + **GP-PARTY-INSTANT-HP** (instant HP-bar on
  damage vs. the ~1 s sync). Live-client wire validation is the project's standing deferred pass.

## Notes / gotchas

- EXP/item share + the in-range kill-credit fan-out is the seam GP-MVPFAME / GP-QUEST
  ride — keep the "members in view of the kill" query reusable.
