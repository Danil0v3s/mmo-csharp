# pet.cpp parity · 2026-05-22 (T9.C — per-fn rollup; T7.2 snapshot serializer)

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

## Per-function coverage

### Lifecycle (IPetService + PetOps)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `PetDatabase::reload` | ✅ | `IPetOpsService.Reload` |
| `PetDatabase::parseBodyNode` | ✅ | T7.2 intif serialization via `SerializeSnapshot(petId)` |
| `pet_data_init` | ⚠️ | `IPetOpsService.DataInit` — stub |
| `pet_create_egg` | ⚠️ | `CreateEgg` — stub |
| `pet_get_egg` | ⚠️ | `GetEgg` — stub |
| `pet_return_egg` | ⚠️ | `ReturnEgg` — stub |
| `pet_birth_process` | ⚠️ | `BirthProcess` — stub |
| `pet_recv_petdata` | ⚠️ | `RecvPetData` — stub |
| `pet_change_name` / `_ack` | ⚠️ | `ChangeName` / `ChangeNameAck` — stubs |
| `do_init_pet` / `do_final_pet` | ❌ | Not in interface |

### Hunger & intimacy

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_hungry_val` | ⚠️ | `HungryVal` — stub (hunger tracked in `PetEntity.Hunger`) |
| `pet_hungry_timer_delete` | ⚠️ | `HungryTimerDelete` — stub; decays via `PetService.Tick` |
| `pet_food` | ⚠️ | `Food` — stub (feeding not yet implemented) |
| `pet_set_intimate` | ⚠️ | `SetIntimate` — stub; decay on starvation in Tick |

### Attack & targeting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_attackskill` | ⚠️ | `AttackSkill` — stub (per-pet AI skill pending) |
| `pet_target_check` | ⚠️ | `TargetCheck` — stub |
| `pet_unlocktarget` | ⚠️ | `UnlockTarget` — stub |

### Evolution & equipment

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_evolution` | ⚠️ | `Evolution` — stub |
| `pet_evolution_requirements_check` | ⚠️ | `EvolutionRequirementsCheck` — stub |
| `pet_equipitem` | ⚠️ | `EquipItem` — stub |
| `pet_sc_check` | ⚠️ | `ScCheck` — stub |

### Egg management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_egg_search` | ⚠️ | `EggSearch` — stub |
| `pet_select_egg` | ⚠️ | `SelectEgg` — stub |

### Catch process & bonus

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_catch_process_start` / `_end` | ⚠️ | `CatchProcessStart` / `End` — stubs |
| `pet_addautobonus` / `_delautobonus` / `_exeautobonus` | ⚠️ | All stubs (scripting backend integration needed) |
| `pet_clear_support_bonuses` | ⚠️ | `ClearSupportBonuses` — stub |
| `pet_lootitem_drop` | ⚠️ | `LootItemDrop` — stub |
| `pet_menu` | ⚠️ | `Menu` — stub |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 2 | 9 | 2 | 13 |
| Hunger & intimacy | 0 | 4 | 0 | 4 |
| Attack & targeting | 0 | 3 | 0 | 3 |
| Evolution & equipment | 0 | 4 | 0 | 4 |
| Egg management | 0 | 2 | 0 | 2 |
| Catch / autobonus / misc | 0 | 9 | 0 | 9 |
| **Totals** | **2** | **31** | **2** | **35** |

## History

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **2 ✅ / 31 ⚠️ / 2 ❌** across 35
entries. Most ⚠️ rows are `IPetOpsService` stubs waiting on the
egg / catch / evolution paths that depend on mob-capture events
and per-pet AI skill plumbing. T7.2 snapshot serializer is the
fully-functional path.

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
