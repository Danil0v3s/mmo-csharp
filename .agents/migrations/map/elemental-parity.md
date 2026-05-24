# elemental.cpp parity · 2026-05-22 (T9.C — per-fn rollup)

`src/map/elemental.cpp` (1149 lines, 19 functions).

All 19 public functions covered by [IElementalService](/Map.Server/Elemental/IElementalService.cs).
AI lives in Mob/; this is the rAthena-name shim.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ElementalDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(elementalId)` |
| `elemental_create` | ⚠️ | `IElementalService.Create` — stub returns 0; per-master entity store gated on PARITY-REMAINING.md §P2.2 (Elemental engine wiring) |
| `elemental_data_received` | ⚠️ | `DataReceived` — stub; gated on §P2.2 |
| `elemental_save` | ⚠️ | `Save` — stub; intif dispatch lights up when live entity exists (§P2.2) |
| `elemental_delete` | ⚠️ | `Delete` — stub; gated on §P2.2 |
| `elemental_dead` | ⚠️ | `Dead` — stub; gated on §P2.2 |
| `do_init_elemental` / `do_final_elemental` | ❌ | Not in interface |

### Mode & targeting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_change_mode` / `_ack` | ⚠️ | `ChangeMode` / `ChangeModeAck` — stubs (§P2.2) |
| `elemental_set_target` | ⚠️ | `SetTarget` — stub (§P2.2) |
| `elemental_unlocktarget` | ⚠️ | `UnlockTarget` — stub (§P2.2) |

### Actions & effects

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_action` | ⚠️ | `Action` — stub (AI rides Mob/ engine; §P2.2) |
| `elemental_clean_effect` | ⚠️ | `CleanEffect` — stub (§P2.2) |
| `elemental_heal` | ⚠️ | `Heal` — stub (§P2.2) |
| `elemental_skillnotok` | ⚠️ | `SkillNotOk` — stub returns false (§P2.2) |

### Lifetime & summon

| rAthena fn | Status | C# location / note |
|---|---|---|
| `elemental_get_lifetime` | ⚠️ | `GetLifetimeMs` — stub returns 0 (§P2.2) |
| `elemental_summon_init` | ⚠️ | `SummonInit` — stub (§P2.2) |
| `elemental_summon_stop` | ⚠️ | `SummonStop` — stub (§P2.2) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 1 | 5 | 2 | 8 |
| Mode & targeting | 0 | 4 | 0 | 4 |
| Actions & effects | 0 | 4 | 0 | 4 |
| Lifetime & summon | 0 | 3 | 0 | 3 |
| **Totals** | **1** | **16** | **2** | **19** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 16 genuine gaps remain)

All 16 ⚠️ entries audited against
[ElementalService.cs](/Map.Server/Elemental/ElementalService.cs); every
listed stub still returns 0 / false / no-op. The whole surface is
gated on the per-master ElementalEntity store + Mob/ AI engine
wiring (PARITY-REMAINING.md §P2.2). Notes refreshed with the
explicit §P2.2 citation; no flips.

### 2026-05-22 — T9.C per-fn rollup

Per-function audit. Baseline: **1 ✅ / 16 ⚠️ / 2 ❌** across 19
entries. The single ✅ is the T7.3 catalog parse / snapshot
serializer. The 16 ⚠️ are `IElementalService` lifecycle / mode /
action stubs waiting on per-master lifetime decay timer + Mob/ AI
engine hook-in. 2 ❌ are do_init / do_final (DI implicit).

### 2026-05-20 — initial audit + service
- 19 functions covered (canonical entry points; data-pending
  on parent dependency).
