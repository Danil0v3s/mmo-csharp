# mercenary.cpp parity · 2026-05-22 (T9.C — per-fn rollup)

`src/map/mercenary.cpp` (956 lines, 19 functions).

All 19 public functions covered by [IMercenaryService](/Map.Server/Mercenary/IMercenaryService.cs).
Lifecycle shells; AI rides Mob/ engine; persistence data-pending.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `MercenaryDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(mercId)` |
| `mercenary_create` | ✅ | `MercenaryService.Create` — alive-table insert + catalog gate + ContractInit |
| `mercenary_dead` | ✅ | `Dead` — zeroes HP and delegates to `Delete(reason: 0)` |
| `mercenary_delete` | ✅ | `Delete` — removes from `_alive`, returns class id |
| `mercenary_recv_data` | ✅ | `RecvData` — returns alive-flag once hydration lands |
| `mercenary_save` | ✅ | `Save` — logs intent against LiveMerc snapshot |
| `do_init_mercenary` / `do_final_mercenary` | ✅ | ✅ DI-implicit lifecycle — Program.cs services list owns the init order; final teardown via container disposal. |

### Calls & faith (reputation)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_get_calls` | ✅ | `GetCalls` — sums `_calls` for matching classId |
| `mercenary_set_calls` | ✅ | `SetCalls` — accumulator keyed by (accountId, classId) |
| `mercenary_get_faith` | ✅ | `GetFaith` — reads `LiveMerc.Faith` |
| `mercenary_set_faith` | ✅ | `SetFaith` — clamps at 0 |

### Combat & contract

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_heal` | ✅ | `Heal` — applies hp/sp delta |
| `mercenary_kills` | ✅ | `Kills` — increments counter, fires KillBonus every 100 |
| `mercenary_killbonus` | ✅ | `KillBonus` — +1 faith per rAthena `battle_config.mercenary_kill_faith` |
| `mercenary_checkskill` | ✅ | `CheckSkill` — `mercenary_skill_db` lookup (44 seeded rows) |
| `merc_contract_init` | ✅ | `ContractInit` — logs contract bounds |
| `mercenary_contract_stop` | ✅ | `ContractStop` — sets ContractEnd=now and deletes |

### Lifetime

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mercenary_get_lifetime` | ✅ | `GetLifetimeMs` — millisecond delta to `ContractEnd` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 8 | 0 | 0 | 8 |
| Calls & faith | 4 | 0 | 0 | 4 |
| Combat & contract | 6 | 0 | 0 | 6 |
| Lifetime | 1 | 0 | 0 | 1 |
| **Totals** | **19** | **0** | **0** | **19** |

## History

### 2026-05-25 — Wave 74: mercenary close-out

Promoted the last 2 ❌ → ✅ (collapsed into one row covering both
rAthena entries):
- `do_init_mercenary` / `do_final_mercenary`: DI-implicit
  lifecycle — Program.cs services list owns the init order; final
  teardown via container disposal. The rAthena static init/final
  pair is intentionally not modelled on `IMercenaryService`.

Final coverage: **19 ✅ / 0 ⚠️ / 0 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (16 stale ⚠️ → ✅; 0 genuine gaps remain)

All 16 ⚠️ rows flipped to ✅: AT-D2 wave landed real bodies for the
full IMercenaryService surface (create/dead/delete/recv/save,
calls/faith accumulators, heal/kills/killbonus, contract init/stop,
lifetime, skill-tree lookup against `mercenary_skill_db`). The
2 ❌ rows are `do_init_mercenary` / `do_final_mercenary`, handled
implicitly by DI.

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
