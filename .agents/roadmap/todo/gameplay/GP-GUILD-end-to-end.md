# GP-GUILD — Guild works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** GP-WOE (castle ownership)

## The deliverable

> A player can **create a guild, invite/accept, set member positions + permissions, edit the
> notice, change the emblem, use guild storage, manage alliances/oppositions, and break the
> guild** — live client, surviving logout.

## Player story

Guilds underpin WoE + endgame social. Today only guild *chat* is wired (README ground truth);
the entire management UI is unreachable.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Entity + migration | ✅ | `Core.Database/Entities/Guild*Entity.cs` |
| Service | partial | guild service + ack handlers exist (member/position model) — verify |
| Persistence IPC | partial | char-side guild RPCs exist; verify each op persists |
| CZ handlers | partial | chat only — invite/expel/position/notice/emblem/storage/alliance missing |
| ZC emits | partial | guild info/members/positions/notice/emblem/alliance/storage missing |

## rAthena reference

- `rathena/src/map/guild.cpp` — `guild_create`, `guild_invite`/`guild_reply_invite`,
  `guild_member_added`, `guild_leave`/`guild_expulsion`, `guild_change_position`/`guild_memberposition`,
  `guild_change_notice`, `guild_change_emblem`, `guild_break`, `guild_alliance`/`guild_opposition`,
  `guild_skillup`, EXP/`guild_getexp`.
- `rathena/src/map/storage.cpp` — `gstorage_*` (guild storage open/add/remove/close, single-opener lock).
- `rathena/src/map/clif.cpp` — the `CZ_REQ_*GUILD*` parse set + `clif_guild_*` emits.
- `char/int_guild.cpp` — persistence (guild, member, position, alliance, emblem, storage rows).

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation (large set — budget for it).
- Guild storage reuses the storage open/lock pattern (verify the storage service exists; extend).

## Scope — every layer

- [ ] **CZ handlers**: create, invite, invite-reply, leave, expel, change-position,
      member-position assign, edit-notice, change-emblem, alliance/opposition req+reply,
      guild-skill-up, storage open/add/remove/close.
- [ ] **Service**: verify the position/permission model; alliance/opposition state machine;
      emblem store; guild EXP + skill points; storage lock.
- [ ] **Persistence**: every op round-trips (member, position, notice, emblem, alliance,
      storage) and survives logout.
- [ ] **ZC emits**: guild info, member list, position info, notice, emblem, alliance list,
      skill info, storage list, message/result codes.

## Done criteria

- A creates a guild, invites B → B accepts → B appears with a position; A edits the notice +
  emblem → B sees them; A opens guild storage, deposits an item → B (with permission) sees it;
  A allies another guild; A breaks the guild → it's gone.
- All of it persists across logout.
- No guild CZ handler / ZC emit missing.

## Test plan

- Handler tests for each CZ → service.
- Service: invite/expel/position/alliance gates, storage lock.
- Persistence round-trips (member/position/notice/emblem/alliance/storage).
- Live: create → invite → position → notice/emblem → storage → alliance → break.

## Notes / gotchas

- Guild storage is single-opener locked (one member at a time) — match rAthena.
- Emblem is a GRF/bitmap blob persisted char-side; store + rebroadcast on view.
- This is the prerequisite for castle ownership in GP-WOE.
