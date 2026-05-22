# pet.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T7.2 snapshot serializer)

`src/map/pet.cpp` (2504 lines, 33 public functions).
Pet lifecycle (create_egg, get_egg, food, attack_skill, evolution, change_name, equip_item, autobonus, catch_process). Real pet AI + lifecycle in Map.Server.Pet; IPetOpsService is the rAthena-name shim.

Canonical entry points: [IPetOpsService](/Map.Server/Pet/PetOps/IPetOpsService.cs)
+ [IPetService](/Map.Server/Pet/IPetService.cs).

## Persistence (intif round-trip) — **T7.2 wave**

| rAthena fn | Status | C# location |
|---|---|---|
| `intif_create_pet` | ✅ | `IntifService.PetCreate` → `ICharServerIpcServicePet.PetCreateAsync` |
| `intif_request_petdata` | ✅ | `IntifService.RequestPetInfo` → `PetLoadAsync` |
| `intif_save_petdata` | ✅ | `IntifService.SavePet` → `IPetService.SerializeSnapshot(petId)` → `PetSaveAsync` (returns 0 if no live pet matches) |
| `intif_delete_petdata` | ✅ | `IntifService.DeletePet` → `PetDeleteAsync` |

The `SerializeSnapshot` walks live pets by persistent `pet_id` and
projects onto `Core.Server.IPC.PetData` (rAthena `pet_data_init` field
shape: id / class / lv / intimacy / hunger / equip / name). See
[intif-parity.md § Pet](intif-parity.md#pet--homunculus--mercenary--elemental--t72--t73-wave)
for the full IIntifService routing.

## History

### 2026-05-22 — T7.2 snapshot serializer

Added `IPetService.SerializeSnapshot(petId)` for the typed-DTO save
path. Walks `_ownerToPet` looking for the matching live
`PetEntity`, projects onto `PetData`. The intif entry points
(PetCreate / RequestPetInfo / SavePet / DeletePet) now all dispatch
through `ICharServerIpcServicePet` instead of returning 0. +5 tests
in [IntifPetWiringTests](/Map.Server.Tests/Services/IntifPetWiringTests.cs).

### 2026-05-20 — initial audit + service
- 33 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
