# FEATURE-27 — Pet egg pet_id binding + create→get-egg response round-trip

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-07 (egg/hatch loop), PACKET-03 (pet UI) · **Blocks:** none

## Problem

FEATURE-07 made `CreateEgg`/`CatchProcessEnd` dispatch `intif_create_pet`, `GetEgg` grant the egg
item, and `BirthProcess` hatch the egg by resolving the **egg item → pet class** (pet_db). But two
links of the rAthena egg loop are still simplified:

1. **pet_id ↔ egg-card binding** — rAthena packs the new `pet_id` into the egg item's card slots
   (`card0/card1`) so a *specific saved pet* (its intimacy/hunger/level) is tied to that egg. FEATURE-07's
   `GetEgg` grants a plain egg item (no pet_id in the cards), and `BirthProcess` hatches a *fresh* pet
   of the egg's class rather than re-binding the saved row. So a caught/levelled pet returned to its
   egg and re-hatched comes back at default intimacy.
2. **create → get-egg round-trip** — `CreateEgg`/`CatchProcessEnd` fire `IntifService.PetCreate`
   (fire-and-forget); the char side inserts the pet row and returns the new `pet_id`, but the char→map
   response that should call `GetEgg(master, class, eggItemId, gender)` to actually place the egg in
   inventory is **not wired** (no response handler). So the egg row is created char-side but the egg
   item never appears for the player until a relog hydrate.

## Current state (C#)

- `Map.Server/Pet/PetOps/PetOpsService.cs` — `CreateEgg` resolves class + dispatches `PetCreate`;
  `GetEgg` grants the egg via `IInventoryService.GiveItem` (no card binding); `BirthProcess` resolves
  class from the egg item + `Summon`s a fresh pet.
- `Map.Server/Services/Intif/IntifService.cs` `PetCreate` (`:513`) is fire-and-forget; no
  create-response handler routes back to `GetEgg`.
- `PetEntity.PetId` / `EggId` exist; the egg `InventoryItem.Card0..3` are not used to carry pet_id.

## rAthena reference (source of truth)

- `rathena/src/map/pet.cpp` `pet_get_egg` — `intif_create_pet` returns pet_id; the egg item is added
  with `card[0]=pet_id` (and the rest of the pet_id high bits) so `pet_birth_process` can look the
  saved row up by pet_id.
- `pet_birth_process` reads the egg's `card[0]`/pet_id, requests the saved row (`intif_request_petdata`),
  and `pet_data_init`s the live pet from it.

## Scope

- [ ] `GetEgg` — grant the egg item with the returned `pet_id` packed into the card slots (match
      rAthena's split). Requires a richer inventory add (cards) — reuse the FEATURE-05 credit path.
- [ ] Wire the `PetCreate` char-response → `GetEgg` (the create round-trip handler), so a created/caught
      egg lands in inventory immediately.
- [ ] `BirthProcess` — when the egg carries a pet_id, request the saved pet row + `pet_data_init`
      (intimacy/hunger/level) instead of a fresh spawn.

## Done criteria

- Catching/creating a pet places the egg in inventory (via the create round-trip), with the pet_id in
  its card slots; hatching restores the saved intimacy/hunger/level.

## Test plan

- `PetLifecycleTests` — GetEgg packs pet_id into cards; BirthProcess of a pet_id-bound egg requests the
  saved row and inits from it.

## Notes / gotchas

- The client pet packets (ZC_PROPERTY_PET / catch UI) are PACKET-03; this ticket is the egg-data
  binding + the create-response wiring, not the client render.
