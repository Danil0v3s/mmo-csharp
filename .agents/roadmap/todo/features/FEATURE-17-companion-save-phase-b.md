# FEATURE-17 — Companion save fan-out Phase B (homunculus / mercenary / elemental)

> **Epic:** Persistence · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** FEATURE-02 (Phase A fan-out + marked slot) · FEATURE-08 (homun live id) ·
> FEATURE-09 (merc live id + non-null snapshot) · FEATURE-10 (elemental persisted id) · **Blocks:** none
> **Filed by:** FEATURE-02 — its Phase A wired quest/achievement/pet saves into the game-loop save
> path and left a clearly-marked Phase-B slot in `PlayerStateService.SaveCompanionsAsync`; the
> homun/merc/elemental saves wait on those entities having real live ids.

## Problem

`PlayerStateService.SaveCompanionsAsync` (FEATURE-02) saves quest + achievement + pet on autosave /
final-save, but the homunculus / mercenary / elemental saves are not wired — they need a live
entity id (FEATURE-08/09/10) and, for mercenary, a non-null `MercenaryService.SerializeSnapshot`.
Until then a player's homunculus level/intimacy, mercenary contract, and elemental are still lost
on logout.

## Current state (C#)

- `Map.Server/Persistence/PlayerStateService.cs:SaveCompanionsAsync` — has the marked
  "Phase B — FEATURE-08/09/10" block (currently empty) following the quest/achievement/pet fan-out.
- `Map.Server/Services/Intif/IntifService.cs` — `HomunculusSave(byte[])` / `MercenarySave(byte[])` /
  `ElementalSave(byte[])` exist (id-header keyed via `BitConverter.ToInt32(data,0)`) but are orphaned.
- `HomunculusService.SerializeSnapshot` / `ElementalService.SerializeSnapshot` are real;
  `MercenaryService.SerializeSnapshot` returns null (FEATURE-09).

## Scope

- [ ] Add `IHomunculusService.TryGetLiveId(PlayerEntity)` / `IMercenaryService.TryGetLiveId` /
      `IElementalService.TryGetLiveId` (mirror `IPetService.TryGetLivePetId`).
- [ ] In the marked Phase-B slot, when each is live, call the matching `IntifService.*Save`
      (fire-and-forget on autosave) and an awaitable `*SaveAsync` on final-save — mirror the Phase-A
      pattern. Pass the 4-byte LE id header so the `byte[]`-keyed wrappers resolve the snapshot.
- [ ] Add awaitable `HomunculusSaveAsync` / `MercenarySaveAsync` / `ElementalSaveAsync` to
      `IIntifService` (the int wrappers delegate), so final-save awaits the row before teardown.

## Done criteria

- On logout, a live homunculus / mercenary / elemental row is saved (awaited) before LeaveMap; a
  relog rehydrates it. `HomunculusSave` / `MercenarySave` / `ElementalSave` are no longer orphaned.

## Test plan

- Extend `CompanionSaveFanoutTests`: with each companion live, final-save invokes its `*SaveAsync`
  once; autosave uses the int wrapper; skipped when not live.
