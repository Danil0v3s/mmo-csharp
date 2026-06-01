# SCRIPT-09 — Companion / mail / channel / battleground builtins (pet, hom, merc, mail, auction, bg, channel)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes · **Priority:** lowest
> **Depends on:** FEATURE-05 (pet), FEATURE-07 (homunculus), FEATURE-08 (mercenary), FEATURE-09 (mail/auction/channel/bg — confirm mapping) · **Blocks:** none

## Problem

The "owned companion + social/queue subsystem" builtins are all stubs. These are the
lowest-priority script surface because few core-town NPCs need them, but instanced events,
pet-handlers, homunculus-evolution NPCs, BG queue NPCs, and mail-reward NPCs do. Today
`ctx.player.pet.*`, `.hom.*`, `.merc.*`, `.mail.*`, `ctx.bg.*`, `ctx.channel.*` log and return
placeholders — pets never spawn, homunculus never evolves, mercenaries never get created, mail
never opens, BG queues never form, and channels never get created/managed from script.

## Current state (C#)

- `Map.Server/Scripting/Dialog/PlayerSubSurfaces.cs:73-117` — `PlayerPetSurface` (catchPet,
  makePet, birthPet, openIncubator, info, skillBonus, skillSupport, skillAttack, skillAttack2,
  recovery, loot), `PlayerHomSurface` (exists, isCalled, info, evolve, morph, mutate, shuffle,
  addIntimacy), `PlayerMercSurface` (create, delete, heal, scStart, …), `PlayerMailSurface`
  (open) — all `ScriptStub`.
- `Map.Server/Scripting/Dialog/SubsystemContexts.cs:87-141` — `BattlegroundContext` (create,
  join, setTeamXY, reserve, unbook, desert, warp, spawnMonster, setMonsterTeam, leave, destroy,
  waitingRoomToBg*, getData, areaUsers, updateScore, info) and `ChannelContext` (create, join,
  setOption, getOption, setColor, setPassword, setGroups, chat, ban, unban, kick, delete) — all stubs.
- Auction: `openauction` builtin (find the JS binding — likely `ctx.player.openAuction()` or on a
  mail/market surface). Mail/auction persist on Char.Server.
- Subsystem dirs that exist map-side: `Map.Server/Pet/`, `Map.Server/Homunculus/`,
  `Map.Server/Mercenary/`, `Map.Server/Mail/`, `Map.Server/BattleGround/`, `Map.Server/Chat/`
  (channels). Verify each is real vs shell — most are FEATURE-* shells.

## rAthena reference (source of truth)

`script.cpp` + `pet.cpp` / `homunculus.cpp` / `mercenary.cpp` / `mail.cpp` / `clif.cpp` (channel) /
`battleground.cpp`.

- **Pet:** `makepet(id)` → `pet_create_egg`(class)/inventory egg; `bpet`/`catchpet` opens the
  catch UI; `petskillbonus`/`petskillsupport`/`petskillattack`/`petskillattack2`/`petrecovery`/
  `petloot` set the active pet's skill behavior block (`pet.cpp pet_skill_*`); `petheal`/`petfriendly`.
- **Homunculus:** `homunculus_evolution`/`morph`/`mutate`/`shuffle` (`homunculus.cpp hom_*`),
  `homunculus_addspiritball`, intimacy ops; `checkhomcall`/`homunculus_exists`.
- **Mercenary:** `mercenary_create(class, lifetime)` (`mercenary.cpp mercenary_create`),
  `mercenary_delete`, `mercenary_heal`, `mercenary_sc_start`, status queries.
- **Mail:** `mail`/`openmail` (`script.cpp`) → `clif_Mail_window` / `mail_send` (system mail with
  zeny + item attachments); persists on Char. `openauction` → `clif_Auction_openwindow`.
- **Battleground:** `bg_create`/`bg_join`/`bg_team_setxy`/`bg_warp`/`bg_monster`/`bg_monster_set_team`/
  `bg_leave`/`bg_destroy`/`bg_get_data`/`bg_getareausers`/`bg_updatescore`/`bg_reserve`/`bg_unbook`/
  `waitingroom2bg*` (`battleground.cpp`).
- **Channel:** `channel_create`/`setopt`/`setcolor`/`setpassword`/`setgroup`/`chat`/`ban`/`unban`/
  `kick`/`delete` (`clif.cpp channel_*`).

## Scope — every sub-system that must be touched

- [ ] **Pet** (`PlayerPetSurface`) → `Map.Server/Pet` service (FEATURE-05): makePet creates the egg,
      birthPet hatches, info returns pet data, skill* set the behavior block, recovery/loot toggles.
      Pet data persists on Char.
- [ ] **Homunculus** (`PlayerHomSurface`) → `Map.Server/Homunculus` (FEATURE-07): evolve/morph/
      mutate/shuffle/addIntimacy, exists/isCalled/info. Persisted on Char.
- [ ] **Mercenary** (`PlayerMercSurface`) → `Map.Server/Mercenary` (FEATURE-08): create/delete/heal/
      scStart + info.
- [ ] **Mail / auction** → `Map.Server/Mail` + Char gRPC: `mail.open()` opens the mailbox window;
      system-mail send with zeny/item attachments; `openAuction()` opens the auction window.
- [ ] **Battleground** (`BattlegroundContext`) → `Map.Server/BattleGround` (FEATURE-09): full set;
      BG team allocation, monster spawns with team tag, score, waiting-room conversion.
- [ ] **Channel** (`ChannelContext`) → `Map.Server/Chat` channel service: create/join/option/color/
      password/group/chat/ban/unban/kick/delete; broadcast channel messages to members.
- [ ] **Client packets** as needed: mail/auction windows, channel join/leave/message, BG UI.

## Done criteria

- `ctx.player.pet.makePet(1002)` gives a Poring egg; hatching spawns the pet; `pet.info(...)`
  returns its stats; `petskillattack` makes the pet use the skill in combat.
- `ctx.player.hom.evolve()` evolves an existing homunculus; `hom.exists()` reflects state.
- `ctx.player.merc.create(2000, 3600)` summons a mercenary for 1 h; `merc.heal(...)` heals it.
- `ctx.player.mail.open()` opens the mailbox; a scripted system mail with a zeny+item attachment
  is received and persists.
- `ctx.bg.create(...)` + `bg.join(...)` form a BG team; `bg.monster(...)` spawns a team-tagged mob;
  `bg.updateScore(...)` updates the scoreboard.
- `ctx.channel.create("#event","Event")` + `channel.chat(...)` creates a channel and broadcasts.
- **No `ScriptStub.Call` left in `PlayerPetSurface`/`PlayerHomSurface`/`PlayerMercSurface`/
  `PlayerMailSurface`/`BattlegroundContext`/`ChannelContext`.**

## Test plan

- `Map.Server.Tests/Scripting/CompanionMailChannelBgBuiltinsTests.cs`: one fixture per subsystem,
  faking the target service; assert the delegate call + emitted packet/IPC. Persisted subsystems
  (pet/hom/merc/mail) get a Char-IPC assertion; channel/bg get a broadcast assertion.

## Notes / gotchas

- HARD-GATED on FEATURE-05/07/08/09 — each subsystem must be real before its builtins can be
  honest. This ticket is the *last* scripting surface to land; do per-subsystem sub-PRs gated on
  each feature ticket. Don't ship a surface that delegates to a shell.
- Pet/hom/merc/mail are Char-persisted: route through gRPC, never a map-local store of record.
- Channel + BG are mostly map-runtime (not persisted) but channels may be config-defined globally —
  confirm against `Map.Server/Chat`.
- `makepet` vs `bpet`/`catchpet`: makepet directly grants the egg (GM/quest reward path); catchpet
  opens the taming UI — keep both distinct.
