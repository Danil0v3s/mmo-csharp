# GP-PET-CATCH-GATES — remaining pet-capture validation gates

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (edge cases)
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Pet capture honours the remaining rAthena gates: the `nopetcapture` mapflag, the mob-hiding
> check, and the "inventory has a free slot" pre-check — so a tame on a no-capture map, against a
> hidden mob, or with a full bag fails cleanly (no lost mob, no orphaned egg row).

## Player story / why it matters

GP-PET (turn 2) implemented `pet_catch_process_end` with the core gates (armed, mob alive, tameable,
class-match, distance) + the HP%-scaled rate. rAthena's `pet_catch_process_end` (pet.cpp:1241) has
three more guards that were not ported because the supporting subsystems aren't cleanly available:

1. **`MF_NOPETCAPTURE` mapflag** — "You can't catch any pet on this map." The C# map model has no
   `nopetcapture` mapflag yet.
2. **`pet_hide_check`** — a mob under SC_HIDING / SC_CLOAKING / SC_CAMOUFLAGE / SC_NEWMOON /
   SC_CLOAKINGEXCEED can't be tamed. Needs the mob's status-effect state queried at catch time.
3. **`pc_inventoryblank`** — the player must have a free inventory slot before the roll; otherwise the
   mob would be removed with nowhere to put the egg. The C# `IInventoryService` has no free-slot/
   blank-count helper yet.

All three are battle-config / edge-case gated; the common "tame a Poring in the field with room in
your bag" path is correct without them.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Service gate (core) | ✅ | `Map.Server/Pet/PetOps/PetOpsService.cs` `CatchProcessEnd` — armed/alive/tameable/class/distance/HP%-rate done |
| nopetcapture mapflag | ☐ | no mapflag on `MapData`; add it + the catch-time check |
| hide check | ☐ | needs mob SC query (SC_HIDING/CLOAKING/CAMOUFLAGE/NEWMOON/CLOAKINGEXCEED) |
| inventory-blank | ☐ | `IInventoryService` needs a free-slot/blank-count helper |

## rAthena reference

- `rathena/src/map/pet.cpp` `pet_catch_process_end` (~1259–1326): the `MF_NOPETCAPTURE`,
  `pc_inventoryblank`, and `battle_config.pet_hide_check` guards (each → `clif_pet_roulette(sd,false)`
  + erase the process).

## Scope — every layer

- [ ] Add a `nopetcapture` mapflag to the map model + check it first in `CatchProcessEnd`.
- [ ] Add an inventory free-slot helper to `IInventoryService`; fail the catch when the bag is full.
- [ ] Query the target mob's hiding/cloaking status effects and fail the catch when hidden.

## Done criteria

- Taming on a `nopetcapture` map → failure roulette, mob unharmed.
- Taming with a full inventory → failure roulette, mob unharmed (no orphaned egg).
- Taming a hidden/cloaked mob → failure roulette.

## Test plan

- Service tests: each gate (nopetcapture map, full inventory, hidden mob) → roulette(false), mob stays,
  no `PetCreate`.

## Notes

- Filed by GP-PET (turn 2). The core capture loop is correct; these are the niche guards.
