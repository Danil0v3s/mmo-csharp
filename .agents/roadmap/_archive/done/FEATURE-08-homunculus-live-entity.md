# FEATURE-08 — Homunculus live entity

> **Epic:** Gameplay-Companion · **Status:** ✅ Done (2026-06-03) · **Size:** XL · **Player-visible:** yes
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

- [x] **New entity** `Map.Server/Entities/HomunculusEntity.cs` — a battle unit (mirror `PetEntity`/`ElementalEntity`): position, HP/SP/MaxHP/MaxSP, stats, mode, `MasterId`, class id, level, the homun id, target. Slot it into `IEntityRegistry` + `IVisibilityService` like pets/elementals.
- [x] Replace (or back) `LiveHomun` with `HomunculusEntity` (or have the service hold the entity and keep `LiveHomun` only for non-spatial bookkeeping). Spawn into the registry on `Call`/`RecvData`, remove on `Vaporize`/`Delete`/`Dead`.
- [x] `Call` — un-vaporize (re-spawn into view) if vaporized; else trigger the homun-load IPC. Emit `clif_spawn` + homun data.
- [x] `RecvData` — build + spawn the `HomunculusEntity` from the char-hydrated row, `status_calc`, place + `clif_spawn` + `clif_hominfo`, start hunger timer. (Currently `:78` only returns 1/0.)
- [x] `Save` — ➡️ the IPC dispatch is **FEATURE-17** (Phase B fan-out; injecting IIntifService here is a DI cycle). Original: call `IntifService.HomunculusSave(...)` (4-byte id header per FEATURE-02). Remove the log-only body.
- [x] `Vaporize` / `Dead` / `Delete` / `Resurrect` / `Revive` — drive the entity vanish/spawn (`NotifyVanishedToArea` / `NotifySpawnedToArea`) in addition to the flag mutation.
- [x] ➡️ **AI + combat** → **FEATURE-29**. Original:: register the homun in `_summonAi` (follow master, assist target) and the attack loop so it actually fights; HP-bar updates via `clif_hominfo`/`clif_homdata` on damage/heal.
- [x] ➡️ **Per-level growth** → **FEATURE-30**. Original:: replace the placeholder `GetMaxHp`/`GetMaxSp` + `GainExp` curve with the real `homunculus_db` growth ranges (`Base*`, growth min/max randomized) and the homun exp table. Load the growth columns in `Reload`.
- [x] ➡️ **Hunger timer** → **FEATURE-31**. Original:: per-homun hunger decay (rAthena `hom_hungry` timer) — intimacy drops when starving, homun reverts/runs at intimacy 0. Tie into the game loop (a `Tick` like `PetService.Tick`).
- [x] ➡️ **Client packets** → **FEATURE-31**. Original:: ZC_PROPERTY_HOMUN / ZC_HO_PAR_CHANGE / ZC_FEED_HOM / ZC_HOSKILLINFO_LIST / ZC_CHANGESTATE_MER (vaporize) etc. Define or use PACKET-* seam; **entity spawn + state must happen here**.
- [x] ➡️ **Save wiring** → **FEATURE-17**. Original: via FEATURE-02 fan-out + at level-up/vaporize.

## Done criteria

> **XL decomposition (2026-06-03):** this card delivers the **entity-existence + lifecycle** slice;
> AI/combat → **FEATURE-29**, growth/exp → **FEATURE-30**, hunger timer + client packets →
> **FEATURE-31**, save IPC → **FEATURE-17** (Phase B fan-out, DI-cycle constrained).

- `Call`/`RecvData` spawns a visible `HomunculusEntity` adjacent to the master, slotted into the
  registry + AOI visibility (HP-bar packet → FEATURE-31). ✅
- The homun follows + attacks the master's target; HP bar updates. ➡️ **FEATURE-29** (AI/combat) + **FEATURE-31** (HP-bar packet).
- Level-up applies real `homunculus_db` growth. ➡️ **FEATURE-30**.
- Vaporize removes the homun from view but keeps the record; `Call` re-summons it. ✅
- `Save` dispatches `IntifService.HomunculusSave`; state persists across relog. ➡️ **FEATURE-17** (the IPC dispatch is a DI cycle from here).
- No bare data-bag `_alive` for spatial state — the live `HomunculusEntity` now holds position/HP and lives in the registry. ✅

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

## History

- 2026-06-03 · XL decomposed into the entity-existence slice + 3 sub-tickets. New
  `Map.Server/Entities/HomunculusEntity.cs` (battle unit mirroring ElementalEntity/PetEntity);
  `HomunculusService` now injects `IEntityRegistry`/`IVisibilityService`/`EntityIdAllocator` and
  spawns the live entity into the registry + AOI on `Call`/`RecvData`, vanishes it on
  `Vaporize`/`Dead`/`Delete` (record kept for vaporize/death; removed on delete), and re-spawns on
  `Resurrect`/`Revive`. The `LiveHomun` record carries the entity reference; the spatial state now
  lives on the entity, not a bare data bag. New `HomunculusSpawnTests` (4) green; full suite 4350
  pass (1 fail = pre-existing INFRA-11). Decomposition follow-ups: FEATURE-29 (AI + combat),
  FEATURE-30 (growth + exp table), FEATURE-31 (hunger timer + client packets); the Save IPC dispatch
  is FEATURE-17 (Phase B fan-out — a direct IIntifService inject here is a DI cycle).
