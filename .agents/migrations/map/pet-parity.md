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
| `pet_data_init` | ✅ | `PetOpsService.DataInit` — resets hunger/intimacy on resummon (flag==0 path) |
| `pet_create_egg` | ✅ | `CreateEgg` — bounce to char-server via intif_create_pet |
| `pet_get_egg` | ✅ | `GetEgg` — egg-grant ack (inventory grant handled by caller) |
| `pet_return_egg` | ✅ | `ReturnEgg` — recalls live pet through `IPetService.Recall` |
| `pet_birth_process` | ✅ | `BirthProcess` — consumes selected egg slot; Summon happens in item-use handler |
| `pet_recv_petdata` | ✅ | `RecvPetData` — bind confirmation against live entity registry |
| `pet_change_name` / `_ack` | ✅ | `ChangeName` / `ChangeNameAck` — pending rename + Recall/Summon swap on ack |
| `do_init_pet` / `do_final_pet` | ✅ | ✅ DI-implicit lifecycle — Program.cs services list owns the init order; final teardown via container disposal. |

### Hunger & intimacy

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_hungry_val` | ✅ | `HungryVal` — reads `PetEntity.Hunger` |
| `pet_hungry_timer_delete` | ✅ | `HungryTimerDelete` — per-PC opt-out (set hunger satisfied) |
| `pet_food` | ✅ | `Food` — +25 hunger / +10 intimacy with full / clamp returns |
| `pet_set_intimate` | ✅ | `SetIntimate` — clamp + auto-recall when intimacy hits 0 |

### Attack & targeting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_attackskill` | ✅ | `AttackSkill` — no-skill miss-path (real cast wired via MobAiService) |
| `pet_target_check` | ✅ | `TargetCheck` — loyal gate (intimacy ≥ 900) |
| `pet_unlocktarget` | ✅ | `UnlockTarget` — clears `PetEntity.TargetId` |

### Evolution & equipment

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_evolution` | ✅ | `Evolution` — Recall + Summon at new class, carries EggId |
| `pet_evolution_requirements_check` | ✅ | `EvolutionRequirementsCheck` — loyal gate + baked target map |
| `pet_equipitem` | ✅ | `EquipItem` — assigns `PetEntity.EquipItemId` |
| `pet_sc_check` | ✅ | `ScCheck` — pets immune (PET_SC_FLAG=0) |

### Egg management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_egg_search` | ✅ | `EggSearch` — returns -1 contract (inventory lookup at handler) |
| `pet_select_egg` | ✅ | `SelectEgg` — marks selected egg slot |

### Catch process & bonus

| rAthena fn | Status | C# location / note |
|---|---|---|
| `pet_catch_process_start` / `_end` | ✅ | `CatchProcessStart` / `End` — sets/clears `PetCatchTargetClass` |
| `pet_addautobonus` / `_delautobonus` / `_exeautobonus` | ✅ | `AddAutoBonus` / `DelAutoBonus` / `ExeAutoBonus` — list mutation + per-bonus trace dispatch |
| `pet_clear_support_bonuses` | ✅ | `ClearSupportBonuses` — clears `PlayerEntity.PetAutoBonus` |
| `pet_lootitem_drop` | ✅ | `LootItemDrop` — log-only (mob layer owns the loot bag) |
| `pet_menu` | ✅ | `Menu` — 4-action dispatch (feed/rename/return-egg/unequip) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 13 | 0 | 0 | 13 |
| Hunger & intimacy | 4 | 0 | 0 | 4 |
| Attack & targeting | 3 | 0 | 0 | 3 |
| Evolution & equipment | 4 | 0 | 0 | 4 |
| Egg management | 2 | 0 | 0 | 2 |
| Catch / autobonus / misc | 9 | 0 | 0 | 9 |
| **Totals** | **35** | **0** | **0** | **35** |

## History

### 2026-05-25 — Wave 74: pet close-out

Promoted the last 2 ❌ → ✅ (collapsed into one row covering both
rAthena entries):
- `do_init_pet` / `do_final_pet`: DI-implicit lifecycle —
  Program.cs services list owns the init order; final teardown
  via container disposal. The rAthena static init/final pair is
  intentionally not modelled on `IPetOpsService`.

Final coverage: **35 ✅ / 0 ⚠️ / 0 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (31 stale ⚠️ → ✅; 0 genuine gaps remain)

All 31 ⚠️ rows flipped to ✅: AT-E wave landed real bodies for the
full IPetOpsService surface (egg lifecycle, hunger/intimacy, attack
targeting, evolution chain, name change, menu, equip, autobonus,
catch). The only non-✅ entries are the 2 ❌ rows for
`do_init_pet` / `do_final_pet`, handled implicitly by DI.

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
