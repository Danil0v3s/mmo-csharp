# FEATURE-02 — Companion / quest / achievement save wiring

> **Epic:** Persistence · **Status:** ✅ Done — Phase A (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none (independent of FEATURE-01; complements FEATURE-03/04/07/08/09/10) · **Blocks:** none

## Problem

The char server has real persistence RPCs (`PetSave`, `HomunculusSave`,
`MercenarySave`, `ElementalSave`, `QuestSave`, `AchievementSave`), and
`Map.Server/Services/Intif/IntifService.cs` wraps every one of them so they
dispatch correctly. **But nothing in the map game loop ever calls those
wrappers.** On periodic autosave and on logout the map only saves *core
character state* (zeny/level/stats/inventory/varregs). A player's pet hunger,
homunculus level/intimacy, mercenary contract timer, elemental, quest progress,
and achievement progress are all **lost on logout / crash** — even the
in-memory state that other features build up. This makes FEATURE-03/04/07–10
pointless without it: progress that can't survive a relog isn't real progress.

## Current state (C#)

- `Map.Server/MapServerImpl.cs:432 AutosaveIfDueAsync` → `:440 SaveAllOnlinePlayersAsync` — iterates spawned sessions and calls only `_playerState.SaveAsync(mapSession, finalSave:false, ct)`.
- `Map.Server/Session/MapSessionLifecycle.cs:86` (logout/leave) — calls `_playerState.SaveAsync(session, finalSave:true, ct)` and the char LeaveMap IPC. No companion/quest/achievement save.
- `Map.Server/Persistence/PlayerStateService.cs:66 SaveAsync` — saves core state (`SaveCoreStateAsync` :103), var-regs, inventory. **No** call to any `IntifService` companion/quest/achievement save. The service does not even inject `IIntifService`.
- `Map.Server/Services/Intif/IntifService.cs` — `QuestSave(pc)` (:462), `AchievementSave(pc)` (:488), `SavePet(int petId)` (:550), `HomunculusSave(byte[])` (:607), `MercenarySave(byte[])` (:669), `ElementalSave(byte[])` (:774) all real and dispatch to char-side RPCs — but **orphaned** (no game-loop caller).
- Snapshot sources already exist: `QuestService.SnapshotFor` (`Quest/QuestService.cs:64`), `AchievementService.SnapshotFor` (`Achievement/AchievementService.cs:67`), `PetService.SerializeSnapshot` (`Pet/PetService.cs:115`), `HomunculusService.SerializeSnapshot` (`Homunculus/HomunculusService.cs:442`), `ElementalService.SerializeSnapshot` (`Elemental/ElementalService.cs:381`). `MercenaryService.SerializeSnapshot` (`Mercenary/MercenaryService.cs:198`) returns `null` (see FEATURE-09).

## rAthena reference (source of truth)

- `rathena/src/map/chrif.cpp` `chrif_save(map_session_data*, int flag)` — the canonical save fan-out. On `CSAVE_NORMAL`/`CSAVE_QUIT` it calls, in order:
  - `pc_makesavestatus` (core state — already ported as `SaveCoreStateAsync`),
  - `intif_save_petdata` (if `sd->status.pet_id && sd->pd`),
  - `homun->save` (if `sd->hd`),
  - `mercenary_save` (if `sd->md`),
  - `elemental_save` (if `sd->ed`),
  - `intif_quest_save`,
  - `intif_achievement_save`,
  - storage save if dirty.
- Autosave (`map.cpp` `map_save_all` → per-PC `chrif_save(sd, CSAVE_AUTOSAVE)`) hits the same fan-out on a timer; quit/disconnect uses `CSAVE_QUIT` (final save) before freeing the session.
- Save-on-state-change: rAthena also forces a pet/homun save at key transitions (hatch, vaporize, level-up) via the same intif calls — periodic save alone is enough for parity but state-change saves reduce loss on crash.
- The C# `MapServerImpl` save loop is structurally the same as `map_save_all`, but it stops at core state — this ticket adds the remaining six calls in the `chrif_save` order.

## Scope — every sub-system that must be touched

- [x] **Phase A** — injected `IIntifService` + `IPetService` into `PlayerStateService` (the existing
      autosave + final-save seam — both route through `SaveAsync`) and added an
      `internal SaveCompanionsAsync(pc, finalSave, ct)` fan-out called at the end of `SaveAsync`,
      mirroring `chrif_save` order (core state, then companions):
  - [x] `IIntifService.QuestSave(pc)` / `QuestSaveAsync(pc)` — always.
  - [x] `IIntifService.AchievementSave(pc)` / `AchievementSaveAsync(pc)` — always.
  - [x] Pet: `IPetService.TryGetLivePetId(pc, out petId)` (new helper) → `SavePet(petId)` /
        `SavePetAsync(petId)` when a pet is live.
- [x] Order + awaiting: added awaitable `QuestSaveAsync` / `AchievementSaveAsync` / `SavePetAsync`
      to `IIntifService` (the int wrappers now delegate to them fire-and-forget). `finalSave:true`
      **awaits** them; autosave uses the fire-and-forget int wrappers.
- [ ] ➡️ **Phase B → FEATURE-17** (deps FEATURE-08/09/10) — homunculus / mercenary / elemental saves. A clearly-marked
      slot is left in `SaveCompanionsAsync`; each is a one-line add once those tickets assign live
      entity ids (and a non-null `MercenaryService.SerializeSnapshot`). The `byte[]`-keyed
      `HomunculusSave/MercenarySave/ElementalSave` id-header mapping lands with them.
- [ ] ➡️ State-change save hooks (pet hatch, homun level-up, merc create) — optional refinement,
      deferred (periodic + final save is the parity floor, which Phase A delivers).

## Done criteria

- ✅ **Phase A**: after autosave / on final-save the quest + achievement + pet char-server saves are
  invoked from the game-loop save path (previously orphaned); final-save awaits them before teardown;
  pet is saved only when one is live. Verified by `CompanionSaveFanoutTests`.
- ➡️ The homunculus / mercenary / elemental saves (and the "all six awaited" wording) are **Phase B → FEATURE-17**,
  gated on FEATURE-08/09/10 (FEATURE-17 does the wiring into the marked slot).
- ✅ `IIntifService.QuestSave` / `AchievementSave` / `SavePet` are each reachable from the game-loop
  save path (no longer orphaned). Homun/merc/elemental remain orphaned until FEATURE-08/09/10.
- ✅ No quest / achievement / pet state is silently dropped on logout.

## Test plan

- `Map.Server.Tests` (add) `CompanionSaveFanoutTests` — mock `IIntifService`; assert that final-save invokes `QuestSave`, `AchievementSave`, and each companion save exactly once when that companion is live, and skips companion saves when none is live.
- Autosave test: advance the autosave timer, assert the fan-out runs for each spawned session.
- Integration (if a char-server harness exists): save → relog → assert hydrated state matches.

## Phasing (so this lands without waiting on FEATURE-08/09/10)

This ticket has a "land now" core and a "land with the companion tickets" tail:

- **Phase A (no dependencies):** wire `QuestSave`, `AchievementSave`, and `SavePet` into the autosave + final-save fan-out. Their snapshot sources (`QuestService.SnapshotFor`, `AchievementService.SnapshotFor`, `PetService.SerializeSnapshot`) are already real — these three save calls can be added immediately and will round-trip real data once FEATURE-03/04/07 fill the mutation paths (and even before, they correctly persist whatever in-memory state exists).
- **Phase B (paired with FEATURE-08/09/10):** add `HomunculusSave` / `MercenarySave` / `ElementalSave` to the same fan-out once those tickets assign real live-entity ids and (for merc) a non-null `SerializeSnapshot`. The fan-out scaffold from Phase A should leave clearly-marked slots for these so Phase B is a one-line add each, not a re-architecture.

## Verification of the orphan claim

Grep confirms the orphan: searching `Map.Server` for callers of the companion save wrappers (`QuestSave`, `AchievementSave`, `SavePet`, `HomunculusSave`, `MercenarySave`, `ElementalSave`) finds only their definitions in `IntifService.cs` and (for some) test references — **no game-loop / lifecycle / persistence caller**. The only save the loop performs is `_playerState.SaveAsync` (core state). This ticket closes that gap.

## Notes / gotchas

- This ticket is the *wiring*; FEATURE-08/09/10 supply the live ids / non-null snapshots that the homun/merc/elemental saves need. The quest/achievement/pet saves can land **immediately** (their snapshot sources are already real).
- Don't introduce a per-save EF transaction across processes — the companion saves are gRPC to the char server, separate from the local `GameDbContext.SaveChangesAsync` for core state.
- Guard against double-save races between autosave and final-save on the same session (the lifecycle sweep already sets `CleanupCompleted`; reuse that flag).
- The legacy `byte[]`-keyed save wrappers (`HomunculusSave(byte[])` etc.) resolve the snapshot by reading the id from `BitConverter.ToInt32(data, 0)` (`IntifService.cs:612/672/777`). The fan-out must pass a 4-byte LE id header so that lookup hits `_xService.SerializeSnapshot(id)` instead of the raw-payload fallback.
- Autosave interval is `MapServerConfiguration.AutosaveInterval` (floored at 30 s, `MapServerImpl.cs:435`); final-save runs in `MapSessionLifecycle.SweepAsync` (`:86`). Both must route through the new fan-out.
- Pet save needs a `pet_id`; `PetService.SerializeSnapshot(petId)` takes the persistent id, so add `IPetService.TryGetLivePetId(PlayerEntity)` (the live pet is one-per-owner, tracked in `_ownerToPet`).

## History

- 2026-06-03 — **Phase A landed.** Wired the orphaned quest / achievement / pet char-server saves
  into the game-loop save path: injected `IIntifService` + `IPetService` into `PlayerStateService`
  and added `SaveCompanionsAsync(pc, finalSave, ct)` (called at the end of `SaveAsync`, the shared
  autosave + final-save seam). Added awaitable `QuestSaveAsync` / `AchievementSaveAsync` /
  `SavePetAsync` to `IIntifService` (the int wrappers delegate fire-and-forget) so final-save awaits
  the row before teardown; added `IPetService.TryGetLivePetId`. `CompanionSaveFanoutTests` (4); full
  Map.Server.Tests 4257 pass (1 fail = pre-existing INFRA-11 replay gate); solution builds.
  Phase B (homunculus / mercenary / elemental) is the marked one-line-each slot, ➡️ FEATURE-08/09/10.
