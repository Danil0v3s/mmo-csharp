# SCRIPT-05 — Party / guild / clan builtins (party_* / guild* / clan_* / warpparty / warpguild)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** Char.Server party/guild/clan gRPC surface (persisted there) · **Blocks:** SCRIPT-06 (instance party/guild checks reuse member queries)

## Problem

Every party/guild/clan script call is a no-op. NPCs that create a party, change a guild
master, warp a whole party/guild to a map (the WoE / GvG / instance staple), or query the
member list to gate entry all do nothing. `ctx.party.*` / `ctx.guild.*` / the clan helpers
log and return placeholders. Because party/guild/clan are **persisted on Char.Server**, these
builtins must go over gRPC — there is no map-local store to mutate.

## Current state (C#)

- `Map.Server/Scripting/Dialog/SubsystemContexts.cs:7-48` — `PartyContext` (`getName`,
  `getMembers`, `getLeader`, `isLeader`, `create`, `destroy`, `addMember`, `delMember`,
  `changeLeader`, `changeOption`) and `GuildContext` (`getName`, `getMaster`, `getMasterId`,
  `info`, `getMembers`, `getSkillLv`, `getAlliance`, `getMapUsers`, `changeMaster`,
  `requestInfo`) — all `ScriptStub.Call`/`CallAsync`.
- Clan: verify the clan helpers (clan_join/clan_leave/clan member count) — likely on
  `WorldContext` or a `ClanContext`; `Map.Server/Clan/` service dir exists.
- `Map.Server/Party/`, `Map.Server/Guild/`, `Map.Server/Clan/` — map-side service dirs exist;
  these are the local caches/forwarders. The authoritative store is Char.Server.
- IPC: `Core.Server/Protos/char_service.proto` already declares party/guild/clan RPCs (per
  `Ipc.md`); `Map.Server/Services/` holds the map→char IPC wrappers to extend.
- `warpparty`/`warpguild` are player-movement ops that fan out over the member list — they need
  the member roster (from Char) + `IWarpService` per online member.

## rAthena reference (source of truth)

`script.cpp` + `party.cpp`/`guild.cpp`/`clan.cpp` + `intif.cpp` (inter-server).

- `script.cpp:23396 BUILDIN(party_create)` → `party_create_byscript` / intif → char DB; returns
  new party id (or 0/-1/-2/-3 on name-taken/already-in-party/etc.). `party_addmember`/
  `party_delmember`/`party_changeleader`/`party_changeoption`/`party_destroy` (`:23441-23575`)
  all route through `intif_party_*` to the char server.
- `script.cpp:8927 BUILDIN(getpartymember)` — fills `$@partymembername$[]` / `$@partymembercid[]`
  / `$@partymemberaid[]` and sets `$@partymembercount`; `type` selects which arrays. For the JS
  API, **return an array of member objects** instead of writing script arrays.
- `script.cpp:9000 BUILDIN(getpartyleader)` — leader field by `type`.
- `script.cpp:5787 BUILDIN(warpparty)` → iterate online party members, `pc_setpos` each to
  (map,x,y); special map keywords "Random"/"SavePoint"/"Leader"/"Leader 0". `flag` controls
  same-map-only / instance handling.
- `guild.cpp` — `getguildmaster`/`getguildmasterid` (`script.cpp:9054`), `getguildmember`
  (`:23854`, fills arrays like getpartymember), `guildchangegm` (`:11163`) → `guild_gm_change`
  via intif. `warpguild` (`:5944`) = warpparty for a guild roster.
- `clan.cpp` — `clan_join`/`clan_leave` (`script.cpp:24170/24187`) attach/detach the player's
  clan id + recalc clan member counts (clan is char-persisted too).

## Scope — every sub-system that must be touched

- [ ] **Map→Char gRPC** in `Map.Server/Services/` (CharServerIpcService-style wrapper): add
      methods for party create/destroy/add/del/changeLeader/changeOption, guild changeMaster,
      clan join/leave, and member-roster fetches (party + guild). Use the existing
      `char_service.proto` RPCs; if a needed RPC is missing, add it to the proto + Char-side
      handler (note the cross-dep but implement it — no stubs).
- [ ] **`PartyContext`** — replace every stub with a gRPC call; `getMembers` returns
      `object[]` of `{charId, accountId, name, online, leader, ...}`; `create` returns the id.
- [ ] **`GuildContext`** — same; `info(guildId,type)` maps to `guild_info` fields;
      `getSkillLv`/`getAlliance`/`getMapUsers` via Char or map-local count.
- [ ] **Clan helpers** — `clan_join`/`clan_leave` + clan member count query.
- [ ] **`warpparty`/`warpguild`** — fetch roster from Char, resolve online members on this map
      server, `IWarpService` each; honor "Random"/"SavePoint"/"Leader" keywords + `flag`.
- [ ] **Live notify** — after a party/guild change, push the relevant `ZC_*` party/guild update
      packets to affected online members (Char broadcasts, or map relays on the IPC response).

## Done criteria

- `await ctx.party.create("Heroes", leaderCharId)` creates a persisted party (visible after relog)
  and returns its id; `getMembers(pid)` returns the roster; `changeLeader`/`changeOption` persist.
- `await ctx.guild.changeMaster(gid, "NewName")` transfers GM (persisted) and notifies members.
- `clan_join`/`clan_leave` change the player's clan and adjust member counts.
- `warpparty("prontera",155,180)` warps every online party member; "Leader" warps to the leader's
  position; only-online members move.
- **No `ScriptStub.Call` left in `PartyContext` / `GuildContext` / the clan helpers.**

## Test plan

- `Map.Server.Tests/Scripting/PartyGuildClanBuiltinsTests.cs`: fake the map→char IPC service;
  invoke each `ctx.party.*`/`ctx.guild.*` and assert the right RPC was issued with the right
  args and the JS return value matches the (faked) RPC response shape.
- `warpparty` test: roster of 3 (2 online here, 1 offline) → exactly 2 `IWarpService` calls.
- Pin `getMembers` array shape (the JS object keys scripts read).

## Notes / gotchas

- Authoritative state is Char.Server — **never** mutate a map-local party/guild cache as the
  source of truth (violates the "no in-memory shortcuts for persisted state" rule). The map
  cache is a read replica refreshed from Char.
- `getpartymember`/`getguildmember` in rAthena write `$@` script arrays; the JS API returns an
  array instead — make sure `scripts/types/api.d.ts` documents the object shape so authors don't
  expect the rAthena array-variable side effect.
- Cross-server warps: members on a *different* map server can't be moved by this map process —
  warpparty only moves members managed locally (parity with rAthena single-map-server assumption,
  but note it explicitly since this stack is multi-process).
