# map.cpp parity · 2026-05-22 (T9.B — per-fn rollup)

`src/map/map.cpp` (5356 lines, 157 public functions).
Map-level utilities (name2mapid, random_cell, foreachpc, foreachmob,
getcell, setmapflag, calc_dir, id2bl, nick2sd). Real entity-registry
iteration + name resolution; specific helpers route through
IMapWorldRegistry / IEntityRegistry.

Canonical entry points: [IMapOpsService](/Map.Server/World/MapOps/IMapOpsService.cs).

## Status legend

- ✅ implemented — full or near-full parity with rAthena
- ⚠️ partial — exists but has documented gaps (typically stub returning sensible default)
- ❌ missing — no C# equivalent

## Per-function coverage

### World registry & name resolution

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_mapname2mapid` | ✅ | `IMapOpsService.Name2MapId` (hash-based) |
| `map_mapid2mapname` | ⚠️ | `MapId2Name` — returns empty; reverse-lookup table pending |
| `map_random_cell` | ⚠️ | `RandomCell` — stub |
| `map_search_freecell_dist` | ⚠️ | `SearchFreeCell` — stub (degrades to (cx, cy)) |
| `map_id2bl` | ✅ | `Id2Bl` — wired to IEntityRegistry |
| `map_charid2sd` | ⚠️ | `CharId2Sd` — degrades to entity-id |
| `map_nick2sd` | ✅ | `Nick2Sd` — LINQ over entity registry |
| `map_addiddb` / `map_deliddb` | ⚠️ | `AddIdDb` / `DelIdDb` — no-ops (entity registry handles ids) |

### Spatial iteration / foreaching

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_foreachpc` | ✅ | `IMapOpsService.ForeachPc` |
| `map_foreachmob` | ✅ | `IMapOpsService.ForeachMob` |
| `map_foreachinmap` | ✅ | `IMapOpsService.ForeachInMap` |
| `map_foreachinrange` / `_inarea` | ✅ | `IMapOpsService.ForeachInRange` (T3.5 splash helper) |
| `map_foreachinallrange` / `_inallarea` | ⚠️ | Alias only — not yet in interface |
| `map_forcountinrange` / `_inarea` | ⚠️ | Count variant — not in interface (use Foreach + count) |

### Block / cell management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_addblock` / `map_delblock` | ⚠️ | Stubs — spatial index transparent |
| `map_moveblock` | ⚠️ | `MoveBlock` — calls `_entities.Move` but returns 0 |
| `map_getcell` | ⚠️ | Always returns true (cell-flag system WIP) |
| `map_setcell` | ⚠️ | No-op |
| `map_cellinfo` / `map_cellchk` | ⚠️ | No cell-type enum yet |

### Map flags & metadata

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_getmapflag` | ⚠️ | Returns 0 (M-H1 flag-service plumbed for 4 gates; full table pending) |
| `map_setmapflag` | ⚠️ | No-op |
| `map_iwall_set` / `_remove` / `_exist` | ❌ | Invisible-wall system not modeled |

### Direction & distance

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_calc_dir` | ✅ | `IMapOpsService.CalcDir` (8-dir) |
| `map_check_dir` | ✅ | `IMapOpsService.CheckDir` |
| `map_random_dir` | ❌ | Not in interface |

### Lifecycle

| rAthena fn | Status | C# location / note |
|---|---|---|
| `do_init_map` / `do_final_map` | ⚠️ | DI handles lifecycle; explicit Init/Final are no-ops |
| `map_reload` | ⚠️ | No-op (reload logic elsewhere) |

### NPC registry & misc

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_addnpc` | ⚠️ | Stub |
| `map_get_new_object_id` | ❌ | Entity-id generation lives on IEntityFactory |
| `map_mapname2ipport` | ❌ | Zone/server routing out of scope |
| `map_addmap` / `map_delmap` | ❌ | Map loading is a separate system |
| Remaining ~120 rAthena fns (debug printers, internal helpers, BL iterators, DB-bookkeeping) | ❌ | Not in interface |

## Coverage summary

The 43-entry table above covers the public surface that gameplay code reaches; the remaining ~114 rAthena fns are internal helpers / debug printers / BL-list bookkeeping that don't need a 1:1 C# port.

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| World registry & name lookup | 3 | 5 | 0 | 8 |
| Spatial iteration | 4 | 2 | 0 | 6 |
| Block / cell mgmt | 0 | 7 | 0 | 7 |
| Map flags | 0 | 2 | 3 | 5 |
| Direction & distance | 2 | 0 | 1 | 3 |
| Lifecycle | 0 | 3 | 0 | 3 |
| NPC / misc / internal helpers | 1 | 1 | 117 | 119 |
| **Totals (gameplay surface)** | **10** | **20** | **4** | **34** |
| **Totals (full file)** | **11** | **20** | **126** | **157** |

The two row sets reflect a deliberate scope split: gameplay surface
(the ~34 functions other Map.Server code calls today) and full file
(every public function in map.cpp, including ~120 internal helpers
without a C# equivalent because the architecture differs).

## History

### 2026-05-22 — T9.B per-fn rollup

Per-function audit. Gameplay-surface baseline: **10 ✅ / 20 ⚠️ /
4 ❌** across 34 entries. Full-file baseline (including internal
helpers): 11 ✅ / 20 ⚠️ / 126 ❌ across 157 entries. Most ⚠️ rows
are stubs returning sensible defaults pending the cell-flag table /
map-flag table / spatial-block index ports.

### 2026-05-20 — initial audit + service
- 157 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
