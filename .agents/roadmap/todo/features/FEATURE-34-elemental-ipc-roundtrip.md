# FEATURE-34 — Elemental create/load/delete IPC round-trip (+ char-side row)

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** M · **Player-visible:** no
> **Depends on:** FEATURE-10 (elemental lifetime + entity) · **Blocks:** none

## Problem

FEATURE-10 added the elemental **lifetime expiry sweep** (despawn on `SummonExpiresAtTick`) and the
real `SerializeSnapshot`. But the elemental's char-server IPC round-trips remain unwired because a
direct `IIntifService` inject into `ElementalService` is a **DI cycle** (`IntifService` already
ctor-depends on `IElementalService` for the snapshot):

1. **Create** doesn't dispatch `IntifService.ElementalCreate` → the char side never allocates a real
   `elemental_id`; `DataReceived` hardcodes `ElementalId = 0`.
2. **DataReceived** builds the entity from a placeholder `MaxHp = master.MaxHp/3` instead of the
   char-hydrated stats (no `ElementalRequest` load round-trip).
3. **Delete / expiry** removes the entity locally but never calls `IntifService.ElementalDelete`
   (the char row lingers).
4. **Save** is a log seam (rides the FEATURE-17 fan-out).

## Current state (C#)

- `Map.Server/Elemental/ElementalService.cs` — `Create`/`DataReceived` spawn locally; `Tick`
  (FEATURE-10) despawns on expiry; `SerializeSnapshot` is real; `Save`/`Delete` don't call IPC.
- `Map.Server/Services/Intif/IntifService.cs` — `ElementalCreate`/`ElementalRequest`/`ElementalSave`/
  `ElementalDelete` are real but orphaned; `IntifService` ctor-injects `IElementalService` (the cycle).

## rAthena reference

- `rathena/src/map/elemental.cpp` — `elemental_create` → `intif_elemental_create`;
  `elemental_data_received` (hydrated stats); `elemental_delete` → `intif_elemental_delete`;
  `elemental_save` → `intif_elemental_save`.

## Scope

- [ ] Break the cycle with a callback/event seam (or route the elemental IPC from a mediator outside
      `ElementalService`), then: `Create` dispatches `ElementalCreate`; the response sets a real
      `ElementalId`; `DataReceived` builds stats from the hydrated payload; `Delete`/expiry calls
      `ElementalDelete`. (Same DI-cycle pattern as FEATURE-17 / FEATURE-22.)
- [ ] Save dispatch rides FEATURE-17 (Phase B fan-out) with the real `ElementalId`.

## Done criteria

- `Create` yields a real char-assigned `elemental_id`; `DataReceived` populates stats from the row;
  `Delete`/expiry removes the char row; state round-trips a save.

## Test plan

- `ElementalServiceTests` — a stubbed char response sets a real `ElementalId`; expiry fires
  `ElementalDelete` once via the seam.
