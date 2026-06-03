# FEATURE-08 — Homunculus live entity

> **Epic:** Gameplay-Companion · **Status:** 🚧 In progress · **Size:** XL · **Player-visible:** yes
> **Depends on:** FEATURE-02 (homun save), FEATURE-01 (kill credit/intimacy) · **Blocks:** none
> **Related:** PACKET-* (homun UI / HP-bar / skill-window packets)

## Problem

The homunculus has rich service bodies (level/intimacy/skill-tree/evolution)
but **it never exists in the world**. `_alive` is a plain `Dictionary<EntityId,
LiveHomun>` of bare data — there is no `HomunculusEntity` spawned into the
spatial index, so the homun has no position, no AI, no HP bar, no combat, and
isn't visible to anyone. `Call` just flips a `Vaporized` bool. `Save` only logs
(never `HomunculusSave`). Per-level growth is a placeholder curve. A player's
homunculus is invisible and inert.

## Current state (C#)

- `Map.Server/Homunculus/HomunculusService.cs`:
  - `_alive` (`:30`) is `Dictionary<EntityId, LiveHomun>` keyed by `master.Id`; `LiveHomun` (`:472`) is a private data bag — **not** an entity in `IEntityRegistry`.
  - `Call(master)` (`:51`) — only flips `live.Vaporized = false`; if no record, returns false. No spawn into the world.
  - `Save(master)` (`:81`) — *log only* (`_logger.LogDebug("hom_save...")`); never calls `IntifService.HomunculusSave`.
  - `Dead` (`:94`), `Vaporize` (`:121`), `Resurrect` (`:105`) flip flags on the data bag; no entity vanish/spawn.
  - `GetMaxHp`/`GetMaxSp` (`:469`) are placeholder linear curves (`100 + (lv-1)*50`).
  - Working data bodies: `LevelUp` (`:181`), `GainExp` (`:192`, naive `lv*1000` curve), intimacy (`:244`–`:256`), skill tree (`:274`–`:329`, DB-sourced), `Evolution` (`:130`), `Menu` (`:378`), `SerializeSnapshot` (`:442`).
- Other companions show the target shape: `PetEntity` (`Map.Server/Entities/`) and `ElementalEntity` are real entities spawned into the registry + visibility. **No `HomunculusEntity` exists.**
- `Map.Server/Services/Intif/IntifService.cs:607 HomunculusSave(byte[])` real but orphaned.
- Summon AI loop exists: `MapServerImpl.cs:307 _summonAi.Tick` ("pets / homunc / mercs / slaves follow their master").

## rAthena reference (source of truth)

- `rathena/src/map/homunculus.cpp`:
  - `hom_call(sd)` — if `sd->hd` vaporized, un-vaporize + `clif_spawn`; else `hom_recv_data` path. Spawns the homun unit next to the master.
  - `hom_recv_data(account_id, struct s_homunculus *sh, int flag)` — char returned the homun row → build the `homun_data` unit (`hom_alloc`), `status_calc_homunculus`, `unit_data` init, place into the map (`map_addblock`), `clif_spawn`, `clif_send_homdata`, `clif_hominfo`, start the hunger timer.
  - `hom_save(hd)` → `intif_homunculus_requestsave`.
  - `hom_levelup(hd)` — per-level stat growth from `homunculus_db` `Base`/`GrowthMin`/`GrowthMax` (randomized growth), recompute status, `clif_hominfo`.
  - `hom_dead(hd)` — set dead, `clif_emotion`, the master keeps the (dead) homun until resurrect.
  - `hom_vaporize(sd, flag)` — `clif_clearunit_area` (remove from view), keep the record.
  - The homun has full AI (`unit_attack`, target following) via the unit system + `mob`-style think; HP bar via `clif_hominfo`/`clif_homdata`.

## Scope — every sub-system that must be touched

- [ ] **New entity** `Map.Server/Entities/HomunculusEntity.cs` — a battle unit (mirror `PetEntity`/`ElementalEntity`): position, HP/SP/MaxHP/MaxSP, stats, mode, `MasterId`, class id, level, the homun id, target. Slot it into `IEntityRegistry` + `IVisibilityService` like pets/elementals.
- [ ] Replace (or back) `LiveHomun` with `HomunculusEntity` (or have the service hold the entity and keep `LiveHomun` only for non-spatial bookkeeping). Spawn into the registry on `Call`/`RecvData`, remove on `Vaporize`/`Delete`/`Dead`.
- [ ] `Call` — un-vaporize (re-spawn into view) if vaporized; else trigger the homun-load IPC. Emit `clif_spawn` + homun data.
- [ ] `RecvData` — build + spawn the `HomunculusEntity` from the char-hydrated row, `status_calc`, place + `clif_spawn` + `clif_hominfo`, start hunger timer. (Currently `:78` only returns 1/0.)
- [ ] `Save` — call `IntifService.HomunculusSave(...)` (4-byte id header per FEATURE-02). Remove the log-only body.
- [ ] `Vaporize` / `Dead` / `Delete` / `Resurrect` / `Revive` — drive the entity vanish/spawn (`NotifyVanishedToArea` / `NotifySpawnedToArea`) in addition to the flag mutation.
- [ ] **AI + combat**: register the homun in `_summonAi` (follow master, assist target) and the attack loop so it actually fights; HP-bar updates via `clif_hominfo`/`clif_homdata` on damage/heal.
- [ ] **Per-level growth**: replace the placeholder `GetMaxHp`/`GetMaxSp` + `GainExp` curve with the real `homunculus_db` growth ranges (`Base*`, growth min/max randomized) and the homun exp table. Load the growth columns in `Reload`.
- [ ] **Hunger timer**: per-homun hunger decay (rAthena `hom_hungry` timer) — intimacy drops when starving, homun reverts/runs at intimacy 0. Tie into the game loop (a `Tick` like `PetService.Tick`).
- [ ] **Client packets**: ZC_PROPERTY_HOMUN / ZC_HO_PAR_CHANGE / ZC_FEED_HOM / ZC_HOSKILLINFO_LIST / ZC_CHANGESTATE_MER (vaporize) etc. Define or use PACKET-* seam; **entity spawn + state must happen here**.
- [ ] **Save wiring** via FEATURE-02 fan-out + at level-up/vaporize.

## Done criteria

- `Call` (or hatch) spawns a visible `HomunculusEntity` adjacent to the master that other players in the AOI can see, with an HP bar.
- The homun follows the master and attacks the master's target (summon AI), taking and dealing damage; its HP bar updates.
- Level-up applies real `homunculus_db` growth (HP/SP/stats within the growth ranges), not the placeholder curve.
- Vaporize removes the homun from view but keeps the record; `Call` re-summons it.
- `Save` dispatches `IntifService.HomunculusSave`; level/intimacy/hunger persist across relog (FEATURE-02).
- No log-only `Save`, no bare data-bag `_alive` for spatial state.

## Test plan

- `Map.Server.Tests` (extend/add `HomunculusServiceTests`):
  - `Call`/`RecvData` add a `HomunculusEntity` to the registry + notify visibility;
  - `Vaporize` removes from view, keeps record; `Call` re-adds;
  - level-up growth lands within the `homunculus_db` min/max range (seeded RNG);
  - `Save` calls `IntifService.HomunculusSave` once.
- Integration with `_summonAi` (homun follows + assists) and FEATURE-02 save.
- Manual/live: summon a homunculus, watch it follow + fight + level, vaporize/call, relog and confirm persistence.

## Notes / gotchas

- This is the heaviest companion ticket because it introduces a brand-new battle entity wired into AI + visibility + combat — budget XL.
- Reuse the `ElementalEntity` spawn/registry pattern (`Elemental/ElementalService.cs:96 DataReceived`) as the template — it's the most complete companion.
- `_alive` is keyed by `master.Id` (EntityId), not char id — keep that key consistent when you add the entity, or you'll desync save/lookup.
- Homun skill tree is already DB-sourced (`_skillTreeFromDb`); the skill *casting* by the live unit is the new part.
- The homun exp curve is a real table (`exp_homunculus`); the current `lv*1000` is a placeholder — load the table.
