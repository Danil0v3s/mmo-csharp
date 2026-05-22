# elemental.cpp parity · 2026-05-22 (T9.C — per-fn rollup)

`src/map/elemental.cpp` (1149 lines, 19 functions).

All 19 public functions covered by [IElementalService](/Map.Server/Elemental/IElementalService.cs).
AI lives in Mob/; this is the rAthena-name shim.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ElementalDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(elementalId)` |
| `elemental_create` | ⚠️ | `IElementalService.Create` — stub (per-master lifetime tracking pending) |
| `elemental_data_received` | ⚠️ | `DataReceived` — stub |
| `elemental_save` | ⚠️ | `Save` — stub (intif dispatch pending live entity) |
| `elemental_delete` | ⚠️ | `Delete` — stub |
| `elemental_dead` | ⚠️ | `Dead` — stub |
| `do_init_elemental` / `do_final_elemental` | ❌ | Not in interface |

### Mode & targeting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_change_mode` / `_ack` | ⚠️ | `ChangeMode` / `ChangeModeAck` — stubs |
| `elemental_set_target` | ⚠️ | `SetTarget` — stub |
| `elemental_unlocktarget` | ⚠️ | `UnlockTarget` — stub |

### Actions & effects

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_action` | ⚠️ | `Action` — stub (AI rides Mob/ engine) |
| `elemental_clean_effect` | ⚠️ | `CleanEffect` — stub |
| `elemental_heal` | ⚠️ | `Heal` — stub |
| `elemental_skillnotok` | ⚠️ | `SkillNotOk` — stub |

### Lifetime & summon

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_get_lifetime` | ⚠️ | `GetLifetimeMs` — stub |
| `elemental_summon_init` | ⚠️ | `SummonInit` — stub |
| `elemental_summon_stop` | ⚠️ | `SummonStop` — stub |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 1 | 5 | 2 | 8 |
| Mode & targeting | 0 | 4 | 0 | 4 |
| Actions & effects | 0 | 4 | 0 | 4 |
| Lifetime & summon | 0 | 3 | 0 | 3 |
| **Totals** | **1** | **16** | **2** | **19** |

## History

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **1 ✅ / 16 ⚠️ / 2 ❌** across 19
entries. The single ✅ is the T7.3 catalog parse / snapshot
serializer. The 16 ⚠️ are `IElementalService` lifecycle / mode /
action stubs waiting on per-master lifetime decay timer + Mob/ AI
engine hook-in. 2 ❌ are do_init / do_final (DI implicit).

### 2026-05-20 — initial audit + service
- 19 functions covered (canonical entry points; data-pending
  on parent dependency).
