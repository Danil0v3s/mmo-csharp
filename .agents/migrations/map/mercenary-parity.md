# mercenary.cpp parity · 2026-05-22 (T9.C — per-fn rollup)

`src/map/mercenary.cpp` (956 lines, 19 functions).

All 19 public functions covered by [IMercenaryService](/Map.Server/Mercenary/IMercenaryService.cs).
Lifecycle shells; AI rides Mob/ engine; persistence data-pending.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `MercenaryDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(mercId)` |
| `mercenary_create` | ⚠️ | `IMercenaryService.Create` — stub |
| `mercenary_dead` | ⚠️ | `Dead` — stub |
| `mercenary_delete` | ⚠️ | `Delete` — stub |
| `mercenary_recv_data` | ⚠️ | `RecvData` — stub (hydration from char-server pending) |
| `mercenary_save` | ⚠️ | `Save` — stub (intif dispatch pending live entity) |
| `do_init_mercenary` / `do_final_mercenary` | ❌ | Not in interface |

### Calls & faith (reputation)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_get_calls` | ⚠️ | `GetCalls` — stub |
| `mercenary_set_calls` | ⚠️ | `SetCalls` — stub |
| `mercenary_get_faith` | ⚠️ | `GetFaith` — stub |
| `mercenary_set_faith` | ⚠️ | `SetFaith` — stub |

### Combat & contract

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_heal` | ⚠️ | `Heal` — stub |
| `mercenary_kills` | ⚠️ | `Kills` — stub |
| `mercenary_killbonus` | ⚠️ | `KillBonus` — stub |
| `mercenary_checkskill` | ⚠️ | `CheckSkill` — stub |
| `merc_contract_init` | ⚠️ | `ContractInit` — stub |
| `mercenary_contract_stop` | ⚠️ | `ContractStop` — stub |

### Lifetime

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_get_lifetime` | ⚠️ | `GetLifetimeMs` — stub |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 1 | 5 | 2 | 8 |
| Calls & faith | 0 | 4 | 0 | 4 |
| Combat & contract | 0 | 6 | 0 | 6 |
| Lifetime | 0 | 1 | 0 | 1 |
| **Totals** | **1** | **16** | **2** | **19** |

## History

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **1 ✅ / 16 ⚠️ / 2 ❌** across 19
entries. The single ✅ is the T7.3 catalog parse / snapshot
serializer. 16 ⚠️ are lifecycle / calls / faith / contract stubs
waiting on the calls/faith reputation tracker + mercenary skill
tree from mercenary_db.yml. 2 ❌ are do_init / do_final (DI
implicit).

### 2026-05-20 — initial audit + service
- 19 functions covered (canonical entry points; data-pending
  on parent dependency).
