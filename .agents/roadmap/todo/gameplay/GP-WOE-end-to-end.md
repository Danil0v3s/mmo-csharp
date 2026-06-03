# GP-WOE — War of Emperium works end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** GP-GUILD (castle ownership) · **Unlocks:** none

## The deliverable

> On the scheduled WoE window, **castle maps activate; guilds fight through guardians to the
> Emperium; breaking the Emperium transfers castle ownership to the breaking guild; the
> economy/treasure + guild-castle data persist** — live client, surviving server restart.

## Player story

The *scheduler* is real (auto start/end on the weekly window, edge-triggered, NPC events fire —
archive FEATURE-15). But there's no castle content: no Emperium entity, no guardians, no
ownership transfer, no can-hit gate for guild-vs-guild on castle maps. So WoE "starts" but
nothing happens on the castle maps.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Scheduler | ✅ verify | `Map.Server/Agit/WoeScheduler.cs` + `IAgitService` (archive FEATURE-15) |
| NPC events | ✅ | `OnAgitStart`/`OnAgitEnd` fire via `EventDoAll` |
| Castle data | ❌ | guild-castle ownership rows + economy/treasure persistence |
| Emperium / guardians | ❌ | the Emperium mob + guardian spawns + break→transfer |
| Can-hit gate | ❌ | GvG/Emperium can-hit gate (archive COMBAT-80) |
| Castle scripts | ❌ | the `agit_controller` + castle NPCs (rides SCR-BULK) |

## rAthena reference

- `rathena/src/map/guild.cpp` — `guild_castledatasave`/`guild_castledataload`,
  `guild_castle_map`, `guild_agit_break` (Emperium broken → `guild_castle->guild_id` set,
  treasure spawn, `OnAgitEnd`/`OnRecvCastle` events).
- `rathena/src/map/mob.cpp` / `npc.cpp` — the Emperium (`MOBID_EMPERIUM`) + guardian mobs;
  `mob_deadhom`-style death → castle transfer.
- `rathena/src/map/battle.cpp` — `battle_check_target`/can-hit GvG + Emperium branch (archive COMBAT-80).
- Castle/treasure scripts in `npc/guild/` (rides SCR-BULK conversion).

## Dependencies — and how to satisfy

- **GP-GUILD** — prerequisite; castle ownership is a guild_id, guardians/treasure key off the
  owning guild. Land guild first.
- Packet-bridge pattern — foundation (castle info / siege state packets).
- Can-hit gate — build the GvG/Emperium `battle_check_target` branch here (absorbs COMBAT-80).
- Castle NPC scripts — the `agit_controller` + per-castle scripts come via SCR-BULK; this ticket
  provides the engine hooks (`OnAgitStart`→spawn Emperium/guardians, `OnAgitEnd`→clear) they call.

## Scope — every layer

- [ ] **Castle data**: guild-castle ownership + economy/treasure rows, load/save (survives restart).
- [ ] **Emperium + guardians**: spawn the Emperium + guardian mobs on `OnAgitStart` for each
      castle; clear on `OnAgitEnd`.
- [ ] **Ownership transfer**: breaking the Emperium sets the castle's owning guild, announces,
      spawns treasure, fires the castle events.
- [ ] **Can-hit gate**: GvG/BG guild-vs-guild + Emperium-can-be-hit gate (archive COMBAT-80).
- [ ] **ZC emits**: castle/siege state, ownership announce.
- [ ] **Engine hooks** the castle scripts call (spawn/clear/transfer entry points).

## Done criteria

- On the scheduled window, a castle map activates with the Emperium + guardians; a guild fights
  in, breaks the Emperium → the castle's owning guild becomes the breaker, treasure spawns, the
  announce fires; outside-guild members can damage on the castle map (can-hit gate).
- Castle ownership + economy persist across a server restart.

## Test plan

- Service: ownership transfer on Emperium death, can-hit gate (GvG/Emperium).
- Scheduler integration: `OnAgitStart` spawns Emperium/guardians (extend archived WoeSchedulerTests).
- Persistence: castle ownership round-trip across restart.
- Live: scheduled start → break Emperium → ownership transfer.

## Notes / gotchas

- The scheduler already fires `OnAgitStart`/`OnAgitEnd` (archive FEATURE-15) — this ticket
  builds what those events drive.
- The castle *scripts* are SCR-BULK; provide stable engine hook entry points so they compose.
