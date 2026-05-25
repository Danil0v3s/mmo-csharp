# elemental.cpp parity · 2026-05-25 (wave 77 close-out)

`src/map/elemental.cpp` (1149 lines, 19 functions).

All 19 public functions covered by [IElementalService](/Map.Server/Elemental/IElementalService.cs).
AI lives in Mob/; this is the rAthena-name shim.

## Per-function coverage

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `ElementalDatabase::parseBodyNode` | ✅ | T7.3 intif serialization via `SerializeSnapshot(elementalId)` ([ElementalService.cs:80](/Map.Server/Elemental/ElementalService.cs)) |
| `elemental_create` | ✅ | [ElementalService.cs:24](/Map.Server/Elemental/ElementalService.cs) — binds `ActiveElementalClassId` + `ActiveElementalExpiresAt` on master; replaces existing per rAthena delete-before-create |
| `elemental_data_received` | ⚠️ | `DataReceived` — stub returns 0; per-master `ElementalEntity` store gated on PARITY-REMAINING §P2.2 |
| `elemental_save` | ⚠️ | `Save` — stub; intif dispatch lights up when live entity exists (§P2.2) |
| `elemental_delete` | ✅ | [ElementalService.cs:47](/Map.Server/Elemental/ElementalService.cs) — clears master's binding; returns 1/0 per rAthena contract |
| `elemental_dead` | ✅ | [ElementalService.cs:55](/Map.Server/Elemental/ElementalService.cs) — delegates to `Delete` |
| `do_init_elemental` / `do_final_elemental` | ✅ | DI-implicit via [Program.cs:501](/Map.Server/Program.cs) (`AddSingleton<IElementalService, ElementalService>`) |

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
| `elemental_get_lifetime` | ✅ | [ElementalService.cs:69](/Map.Server/Elemental/ElementalService.cs) — returns remaining ms from `ActiveElementalExpiresAt` minus `TickCount64`, clamped at 0 |
| `elemental_summon_init` | ⚠️ | `SummonInit` — stub (§P2.2) |
| `elemental_summon_stop` | ✅ | [ElementalService.cs:77](/Map.Server/Elemental/ElementalService.cs) — delegates to `Delete` (rAthena's stop-on-despawn pattern) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Lifecycle | 5 | 2 | 0 | 7 |
| Mode & targeting | 0 | 4 | 0 | 4 |
| Actions & effects | 0 | 4 | 0 | 4 |
| Lifetime & summon | 2 | 1 | 0 | 3 |
| **Totals** | **7** | **11** | **0** | **18** |

(`do_init_elemental` + `do_final_elemental` are counted as one DI-implicit row.)

## History

### 2026-05-25 — Wave 77: elemental-parity close-out (5 ⚠️→✅, 1 ❌→✅)

Honest re-audit of [ElementalService.cs](/Map.Server/Elemental/ElementalService.cs)
against the existing source — five rows that had real bodies were
stale-tagged as ⚠️:

- `elemental_create` (line 24) — touches `PlayerEntity.ActiveElementalClassId` +
  `ActiveElementalExpiresAt`, honors rAthena's delete-before-create.
- `elemental_delete` (line 47) — clears master binding, returns 1/0.
- `elemental_dead` (line 55) — delegates to Delete.
- `elemental_get_lifetime` (line 69) — real arithmetic over tick deltas.
- `elemental_summon_stop` (line 77) — delegates to Delete.

`do_init_elemental` / `do_final_elemental` flipped ❌ → ✅ — DI-implicit
via `Program.cs:501` (`AddSingleton<IElementalService, ElementalService>`),
matching the convention for every other rAthena init/final pair.

**Residual gates**: 11 ⚠️ remain (data_received / save / change_mode×2 /
clean_effect / action / set_target / unlock_target / heal / skillnotok /
summon_init) — all genuinely stubbed, all waiting on the per-master
`ElementalEntity` store + Mob/ AI engine hook-in (PARITY-REMAINING §P2.2).

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
