# PACKET-02-guild — Guild client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-guild (GuildService + Intif guild RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Guild/GuildService.cs` exposes the full rAthena `guild.cpp` surface
(`Invite`, `ReplyInvite`, `Leave`, `Expulsion`, `ChangePosition`, `ChangeNotice`,
`ChangeEmblem`, `Break`, `AllianceAck`, `SkillUp`, …) and `IIntifService` has the matching
persistence RPCs (`GuildAddMember`, `GuildLeave`, `GuildExpulsion`, `GuildBreak`,
`GuildEmblem`, `GuildSavePosition`, `GuildSetSkill`, `GuildAllianceAck`). But the **only**
guild packet handled today is `CZ_GUILD_CHAT` (`Map.Server/Handlers/Chat/GuildChatHandler.cs`).
A player cannot invite, expel, change a position, edit the notice, change the emblem, open
guild storage, break the guild, or manage alliances from the client.

## Current state (C#)

- `Map.Server/Handlers/Chat/GuildChatHandler.cs` — `CZ_GUILD_CHAT = 0x017e`, only guild packet wired.
- `Map.Server/Guild/IGuildService.cs` — `Invite(inviter, invitee)`, `ReplyInvite(invitee, guildId, ok)`,
  `Leave`, `Expulsion(gm, guildId, accountId, charId, reason)`, `ChangePosition(gm, idx, mode, exp_mode, name)`,
  `ChangeMemberPosition`, `ChangeNotice(pc, guildId, mes1, mes2)`, `ChangeEmblem(pc, data)`,
  `EmblemChanged`, `Break(gm, name)`, `AllianceAck`, `SkillUp(pc, skillId)`, `RecvInfo`.
- `Map.Server/Storage/Guild/IGuildStorageService.cs` — guild storage open/close service.
- `Map.Server/Services/Intif/IIntifService.cs:50-61` — `GuildCreate`, `GuildRequestInfo`,
  `GuildAddMember`, `GuildLeave`, `GuildExpulsion`, `GuildBreak`, `GuildMessage`, `GuildEmblem`,
  `GuildSavePosition`, `GuildSetSkill`, `GuildAllianceAck`, `GuildAddCastle`.
- `Map.Server/Services/Intif/IIntifService.cs:106` — `RequestGuildStorage(charId, guildId)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp` parse functions to port:

- `clif_parse_GuildInvite` → `guild_invite` → emits `ZC_REQ_JOIN_GUILD` on target.
- `clif_parse_GuildReplyInvite` → `guild_reply_invite`.
- `clif_parse_GuildLeave` → `guild_leave`.
- `clif_parse_GuildExpulsion` → `guild_expulsion`.
- `clif_parse_GuildChangePositionInfo` → `guild_change_position` (define a position: mode/exp/name).
- `clif_parse_GuildChangeMemberPosition` → `guild_change_memberposition` (assign member to a position).
- `clif_parse_GuildBreak` → `guild_break` (master only, name confirm).
- `clif_parse_GuildChangeNotice` → `guild_change_notice` (mes1 60B + mes2 120B).
- `clif_parse_GuildChangeEmblem` → `guild_change_emblem` (raw bitmap bytes) and
  `clif_parse_GuildRequestEmblem` → send emblem to requester.
- `clif_parse_GuildRequestInfo` → `guild_recv_info` request (tab open).
- `clif_parse_GuildRequestAlliance` → `guild_reqalliance`; `clif_parse_GuildReplyAlliance`
  → `guild_reply_reqalliance`; `clif_parse_GuildOpposition` → `guild_opposition`;
  `clif_parse_GuildDelAlliance` → `guild_delalliance`.
- `clif_parse_GuildCheckMaster` — master-check ping (informational ack).
- Guild storage open: handled in `storage.cpp` via the NPC `guildopenstorage` path; the client
  open packet is `CZ_REQ_OPEN_MEMBER_INFO`/storage open — confirm against `clif_packetdb.hpp`.

ZC responses: `ZC_REQ_JOIN_GUILD` (invite popup), `ZC_ACK_REQ_JOIN_GUILD`,
`ZC_UPDATE_GDID` / `ZC_GUILD_INFO`, `ZC_POSITION_INFO` / `ZC_MEMBERMGR_INFO`,
`ZC_GUILD_NOTICE`, `ZC_CHANGE_GUILD` (emblem version bump), `ZC_REQ_ALLY_GUILD`,
`ZC_ACK_REQ_ALLY_GUILD`, `ZC_ACK_LEAVE_GUILD` / `ZC_ACK_BAN_GUILD`, `ZC_GUILD_EMBLEM_IMG`.

**Read `clif_packetdb.hpp`** for every numeric id (PACKETVER-shuffled). Do not fabricate ids.

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`): `CZ_REQ_JOIN_GUILD`, `CZ_JOIN_GUILD`
      (reply), `CZ_REQ_LEAVE_GUILD`, `CZ_REQ_BAN_GUILD` (expel), `CZ_REG_CHANGE_GUILD_POSITIONINFO`
      (var-len, position table), `CZ_REQ_CHANGE_MEMBERPOS` (var-len), `CZ_GUILD_NOTICE`
      (mes1.60B mes2.120B), `CZ_REGISTER_GUILD_EMBLEM` (var-len raw bytes), `CZ_REQ_GUILD_EMBLEM_IMG`,
      `CZ_GUILD_DISORGANIZE` (break, with name), `CZ_REQ_ALLY_GUILD`, `CZ_ALLY_GUILD` (reply),
      `CZ_REQ_HOSTILE_GUILD` (opposition), `CZ_REQ_DELETE_RELATED_GUILD` (del alliance),
      `CZ_REQ_GUILD_MENU` / open-storage request.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`): `ZC_REQ_JOIN_GUILD`, `ZC_ACK_REQ_JOIN_GUILD`,
      `ZC_GUILD_NOTICE`, `ZC_POSITION_INFO`, `ZC_MEMBERMGR_INFO`, `ZC_CHANGE_GUILD` (emblem bump),
      `ZC_REQ_ALLY_GUILD`, `ZC_ACK_REQ_ALLY_GUILD`, `ZC_ACK_LEAVE_GUILD`, `ZC_ACK_BAN_GUILD`,
      `ZC_GUILD_EMBLEM_IMG`.
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (var-len entries).
- [ ] **Handlers** (`Map.Server/Handlers/Guild/`):
  - [ ] `GuildInviteHandler` → `IGuildService.Invite`.
  - [ ] `GuildReplyInviteHandler` → `IGuildService.ReplyInvite`.
  - [ ] `GuildLeaveHandler` → `IGuildService.Leave` → `IIntifService.GuildLeave`.
  - [ ] `GuildExpelHandler` → `IGuildService.Expulsion` → `IIntifService.GuildExpulsion`.
  - [ ] `GuildChangePositionHandler` → `IGuildService.ChangePosition` → `IIntifService.GuildSavePosition`.
  - [ ] `GuildChangeMemberPositionHandler` → `IGuildService.ChangeMemberPosition`.
  - [ ] `GuildNoticeHandler` → `IGuildService.ChangeNotice`.
  - [ ] `GuildEmblemHandler` → `IGuildService.ChangeEmblem` → `IIntifService.GuildEmblem`;
        `GuildRequestEmblemHandler` → emit `ZC_GUILD_EMBLEM_IMG`.
  - [ ] `GuildBreakHandler` → `IGuildService.Break` → `IIntifService.GuildBreak`.
  - [ ] `GuildAllianceRequestHandler` / `GuildAllianceReplyHandler` / `GuildOppositionHandler` /
        `GuildDelAllianceHandler` → `IGuildService.AllianceAck` → `IIntifService.GuildAllianceAck`.
  - [ ] `GuildStorageOpenHandler` → `IGuildStorageService` open + `IIntifService.RequestGuildStorage`.
  - [ ] `GuildRequestInfoHandler` → `IGuildService.RecvInfo` / `IIntifService.GuildRequestInfo`.
- [ ] All persistence RPCs already exist — no new char-side proto.

## Done criteria

- Each guild action (invite/expel/leave/position-define/member-assign/notice/emblem/break/
  alliance/oppose/storage-open) succeeds end-to-end and produces the correct ZC ack.
- Permission gates match rAthena: only master may break/define-positions/change-emblem; only
  members with the relevant permission bit may invite/expel/edit-notice (`HasPermission`).
- Emblem change bumps the emblem version and broadcasts `ZC_CHANGE_GUILD` to viewers.
- Guild storage opens via `RequestGuildStorage` and reuses the existing storage notifier packets.
- No stub, no `// TODO`, no log-only no-op.

## Test plan

- Handler tests pinning the permission gate per action and the exact `IIntifService`/`IGuildService`
  call with correct args (esp. notice mes1/mes2 lengths, position mode/exp encoding).
- Pin expel-vs-leave flag byte.
- Manual: two-guild alliance request/accept; emblem upload visible to other members.

## Notes / gotchas

- Emblem is a raw bitmap blob — the In packet is variable-length; preserve bytes verbatim, do not
  re-encode. `CheckEmblemChangeCondition` gates min interval / GM settings.
- Guild storage shares the storage notifier (`Map.Server/Handlers/Storage/StorageNotifier.cs`) and
  the existing `CZ_MOVE_ITEM_FROM_*` packets — only the *open* path is new here.
- Alliance has four distinct flows (request/reply/oppose/delete); do not collapse them.
