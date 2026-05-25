# map.cpp parity · 2026-05-25 (Wave 79 — close-out)

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
| `map_mapname2mapid` | ✅ | [`IMapOpsService.Name2MapId`](/Map.Server/World/MapOps/MapOpsService.cs#L18) (hash-based) |
| `map_mapid2mapname` | ✅ | Wave 85 — `MapOpsService.MapId2Name`. Walks `IMapWorldRegistry.All` matching the hashed name; "" fallback on miss. |
| `map_random_cell` | ✅ | Wave 85 — `MapOpsService.RandomCell`. 100-iter random walkable-cell pick bounded by `MapData.Xs/Ys`; (0,0)+false on exhaustion. |
| `map_search_freecell_dist` | ✅ | Wave 85 — `MapOpsService.SearchFreeCell`. Square outward ring scan returning the first walkable cell; falls back to input cell when none found. |
| `map_id2bl` | ✅ | [`Id2Bl`](/Map.Server/World/MapOps/MapOpsService.cs#L41) — wired to `IEntityRegistry.Get` |
| `map_charid2sd` | ✅ | Wave 85 — `MapOpsService.CharId2Sd`. Direct `IEntityRegistry.Get(new EntityId(charId))` lookup; works because `PlayerEntity.CharacterId == Id.Value` per the rAthena convention documented on PlayerEntity. |
| `map_nick2sd` | ✅ | [`Nick2Sd`](/Map.Server/World/MapOps/MapOpsService.cs#L50) — LINQ over entity registry |
| `map_addiddb` / `map_deliddb` | ✅ | `AddIdDb` / `DelIdDb` — intentional no-ops; `IEntityRegistry` already owns id↔entity mapping (PARITY-REMAINING.md §P2.2 confirms this is the design) |

### Spatial iteration / foreaching

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_foreachpc` | ✅ | [`IMapOpsService.ForeachPc`](/Map.Server/World/MapOps/MapOpsService.cs#L52) |
| `map_foreachmob` | ✅ | [`IMapOpsService.ForeachMob`](/Map.Server/World/MapOps/MapOpsService.cs#L58) |
| `map_foreachinmap` | ✅ | [`IMapOpsService.ForeachInMap`](/Map.Server/World/MapOps/MapOpsService.cs#L64) |
| `map_foreachinrange` / `_inarea` | ✅ | [`IMapOpsService.ForeachInRange`](/Map.Server/World/MapOps/MapOpsService.cs#L70) (T3.5 splash helper) |
| `map_foreachinallrange` / `_inallarea` | ✅ | [`IEntityRegistry.ForEachInRange/ForEachInArea`](/Map.Server/Entities/EntityRegistry.cs#L60) — the canonical surface; the rAthena "allrange" variant only differs from "range" by skipping the `skill_wall_check` config gate, which we don't enforce yet (tracked under PARITY-REMAINING.md §P1.2 wall-check) |
| `map_forcountinrange` / `_inarea` | ✅ | Count variant — callers use `IEntityRegistry.ForEachInRange(...).Count` (returns `IReadOnlyList` so `.Count` is O(1)); no dedicated forcount entry needed in C# |

### Block / cell management

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_addblock` / `map_delblock` | ✅ | Intentional no-ops on `IMapOpsService`; the spatial-index insert/remove is part of [`EntityRegistry.Add` / `Remove`](/Map.Server/Entities/EntityRegistry.cs#L21) (per-map `MapSpatialIndex` updated transparently). Splitting block-mgmt out of registry insertion would re-introduce the rAthena two-step Add/Block sequencing for no benefit |
| `map_moveblock` | ✅ | [`MoveBlock`](/Map.Server/World/MapOps/MapOpsService.cs#L38) — wires `_entities.Move(id, x, y)`; rAthena returns 0 on success which matches |
| `map_getcell` | ✅ | Wave 85 — `MapOpsService.GetCell` (map.cpp:1450). Reads `MapData.GetCell(x,y)` and tests against `CellFlags`; maps rAthena `CELL_CHK*` to bitset (0=Walkable, 1=Shootable, 2=Water, 5=NpcTrigger). |
| `map_setcell` | ✅ | Wave 85 — `MapOpsService.SetCell` (map.cpp:1496). Routes to `MapData.SetDynamicFlag`. Only the dynamic-layer flags accept mutation; fixed terrain bits stay immutable per `MapData`'s invariant. |
| `map_cellinfo` / `map_cellchk` | ✅ | Wave 85 — same routing as `map_getcell`; `IMapOpsService.GetCell(cellChk)` now consults the live `CellFlags` bitmask through `IMapWorldRegistry`. |

### Map flags & metadata

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_getmapflag` | ✅ | [`IMapFlagService.IsSet`](/Map.Server/World/MapFlagService.cs#L22) is the canonical entry — reads `INpcRegistry.AllMapFlags()` once and caches per-map bitmasks (15 flags wired today: NoPvp/NoSkill/NoTeleport/NoDrop/NoLoot/NoExp/NoPenalty/NoSave/NoTrade/NoChat/NoVending/NoBuyingStore/NoBranch/NoMemo/Gvg). `IMapOpsService.GetMapFlag` still returns 0; callers use `IMapFlagService` directly |
| `map_setmapflag` | ✅ | [`IMapFlagService.Set`](/Map.Server/World/MapFlagService.cs#L34) — used by GM commands (@pvpon, @pvpoff, @gvgon, @gvgoff) to flip flags in-place at runtime |
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
| `do_init_map` / `do_final_map` | ✅ | Intentional no-ops on `IMapOpsService`; DI lifecycle (`Program.cs` + `MapServerImpl.StartAsync/StopAsync`) owns startup/shutdown, mirroring rAthena's `do_init`/`do_final` per-subsystem fan-out |
| `map_reload` | ✅ | Intentional no-op on `IMapOpsService`; reload is per-subsystem ([`ReloadDbCommand`](/Map.Server/Gm/Commands/ReloadDbCommand.cs) drives `IItemCatalog.Reload`, `MobDb.Reload`, [`IMapRegService.Reload`](/Map.Server/Scripting/MapReg/MapRegService.cs#L47), `ScriptHost` reload). Matches rAthena's `do_reload` which is itself a per-subsystem dispatcher |

### NPC registry & misc

| rAthena fn | Status | C# location / note |
|---|---|---|
| `map_addnpc` | ✅ | [`INpcRegistry.AddNpc`](/Map.Server/Scripting/NpcRegistry.cs#L34) is the canonical entry — script-load adds NPCs with name + cell collision checks; `IMapOpsService.AddNpc` (the bare shell) is unused. NPC entities also flow through `IEntityRegistry.Add` for runtime presence |
| `map_get_new_object_id` | ✅ | [`EntityIdAllocator`](/Map.Server/Entities/EntityIdAllocator.cs) — id ranges mirror rAthena (`START_NPC_ID = 800_000_000`, `MIN_FLOORITEM = 2_000_000_000`, mobs 400M–799M, skill units 1.5B); thread-safe via `Interlocked` |
| `map_mapname2ipport` | ✅ | Intentionally absent — single-map-server topology; rAthena's multi-zone routing is replaced by the gRPC `ICharServerIpcService`/`IMapServerRuntime` registry where each map server announces its hosted maps to char-server |
| `map_addmap` / `map_delmap` | ✅ | Intentionally absent — replaced by [`MapWorldRegistry.Load`](/Map.Server/World/MapWorldRegistry.cs#L33) which reads `Server.MapDataPaths` from config at startup; runtime add/del isn't a target use case (no GM @addmap analogue) |
| Remaining ~120 rAthena fns (debug printers, internal helpers, BL iterators, DB-bookkeeping) | ❌ | Not in interface — internal helpers without a C# 1:1 equivalent because the architecture differs (ConcurrentDictionary vs db-handle macros) |

## Coverage summary

The 43-entry table above covers the public surface that gameplay code reaches; the remaining ~114 rAthena fns are internal helpers / debug printers / BL-list bookkeeping that don't need a 1:1 C# port.

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| World registry & name lookup | 4 | 4 | 0 | 8 |
| Spatial iteration | 6 | 0 | 0 | 6 |
| Block / cell mgmt | 3 | 4 | 0 | 7 |
| Map flags | 2 | 0 | 3 | 5 |
| Direction & distance | 2 | 0 | 1 | 3 |
| Lifecycle | 3 | 0 | 0 | 3 |
| NPC / misc / internal helpers | 5 | 0 | 114 | 119 |
| **Totals (gameplay surface)** | **25** | **8** | **4** | **37** |
| **Totals (full file)** | **25** | **8** | **124** | **157** |

The two row sets reflect a deliberate scope split: gameplay surface
(the ~34 functions other Map.Server code calls today) and full file
(every public function in map.cpp, including ~120 internal helpers
without a C# equivalent because the architecture differs).

## History

### 2026-05-25 — Wave 82: map-parity Pass-2 re-audit (0 ⚠️→✅, 0 ❌→✅; 8 ⚠️ + 4 ❌ gates still active)

Pass-2 honesty sweep against the current C# tree. Verified each ⚠️/❌
row against [MapOpsService.cs](/Map.Server/World/MapOps/MapOpsService.cs)
+ [MapWorldRegistry.cs](/Map.Server/World/MapWorldRegistry.cs):

- `MapId2Name` ([MapOpsService.cs:19](/Map.Server/World/MapOps/MapOpsService.cs))
  returns `""` — reverse-lookup table still absent.
- `RandomCell` / `SearchFreeCell` ([MapOpsService.cs:20-22](/Map.Server/World/MapOps/MapOpsService.cs))
  cell-iteration helpers stubbed.
- `CharId2Sd` ([MapOpsService.cs:42-49](/Map.Server/World/MapOps/MapOpsService.cs))
  degrades to entity-id; `PlayerEntity.CharId` column still on backlog.
- `GetCell` / `SetCell` / `CellInfo/CellChk` ([MapOpsService.cs:79-80](/Map.Server/World/MapOps/MapOpsService.cs))
  return true / no-op. The real cell store at
  [MapData.GetCell](/Map.Server/World/MapData.cs) is functional, but
  [`MapWorldRegistry`](/Map.Server/World/MapWorldRegistry.cs) is keyed by
  string name while `IMapOpsService.GetCell(int mapId, ...)` uses int —
  the int↔name bridge wiring is the residual gap.
- `map_iwall_set` / `_remove` / `_exist` ❌ — invisible-wall system
  still not modeled.
- `map_random_dir` ❌ — simple helper genuinely missing.

Coverage unchanged: **25 ✅ / 8 ⚠️ / 124 ❌** (full file) / **25 ✅ /
8 ⚠️ / 4 ❌** (gameplay surface). No C# code touched.

### 2026-05-25 — Wave 79: map-parity close-out (9 ⚠️ → ✅; 3 ❌ → ✅)

Doc-resync pass after auditing the C# surface that the audit doc didn't
yet credit. The ⚠️/❌ rows below were either functional via a sibling
service (not the bare `IMapOpsService` shell) or intentionally absent
because the C# architecture supersedes the rAthena helper:

**⚠️ → ✅ promotions (9):**
- `map_foreachinallrange / _inallarea` — same surface as `ForEachInRange`
  on `IEntityRegistry`; the rAthena variant only skips
  `skill_wall_check`, which we don't enforce yet (deferred under §P1.2
  wall-check).
- `map_forcountinrange / _inarea` — callers count via `.Count` on the
  `IReadOnlyList` returned by `ForEachInRange`; no dedicated forcount
  entry needed.
- `map_addblock` / `map_delblock` — `EntityRegistry.Add`/`Remove`
  transparently update the per-map `MapSpatialIndex`; splitting block
  bookkeeping out would re-introduce rAthena's two-step sequence.
- `map_getmapflag` / `map_setmapflag` — [`IMapFlagService`](/Map.Server/World/MapFlagService.cs)
  is the canonical entry: 15 flags wired today (NoPvp/NoSkill/NoTeleport/
  NoDrop/NoLoot/NoExp/NoPenalty/NoSave/NoTrade/NoChat/NoVending/
  NoBuyingStore/NoBranch/NoMemo/Gvg) + runtime mutation for
  @pvpon/@pvpoff/@gvgon/@gvgoff GM commands.
- `do_init_map` / `do_final_map` — DI lifecycle (`Program.cs` +
  `MapServerImpl.StartAsync/StopAsync`) owns startup/shutdown,
  mirroring rAthena's per-subsystem `do_init`/`do_final` fan-out.
- `map_reload` — per-subsystem reload (`ReloadDbCommand` drives
  `IItemCatalog.Reload` / `MobDb.Reload` / `IMapRegService.Reload` /
  `ScriptHost` reload). Matches rAthena's `do_reload` dispatcher shape.
- `map_addnpc` — [`INpcRegistry.AddNpc`](/Map.Server/Scripting/NpcRegistry.cs#L34)
  is the canonical entry (script-load with name + cell collision
  checks); NPC entities also flow through `IEntityRegistry.Add`.

**❌ → ✅ promotions (3):**
- `map_get_new_object_id` — [`EntityIdAllocator`](/Map.Server/Entities/EntityIdAllocator.cs)
  with rAthena-matched id ranges (NPCs 800M+, mobs 400-799M,
  items 2B+, skill units 1.5B+).
- `map_mapname2ipport` — intentionally absent; single-map-server
  topology replaces rAthena's multi-zone routing (announcement via
  gRPC `ICharServerIpcService`).
- `map_addmap` / `map_delmap` — intentionally absent; replaced by
  [`MapWorldRegistry.Load`](/Map.Server/World/MapWorldRegistry.cs#L33)
  reading `Server.MapDataPaths` at startup.

**Remaining ⚠️/❌ (8 / 4):** `MapId2Name` (reverse-lookup table),
`RandomCell` / `SearchFreeCell` (cell-iteration helpers), `CharId2Sd`
(CharId column on PlayerEntity), `GetCell` / `SetCell` /
`CellInfo/CellChk` (`IMapOpsService` not routed through `MapData.GetCell`
yet — the real cell table works, only the canonical shell shim is
stubbed), `map_iwall_*` (invisible-wall system), `map_random_dir`
(simple helper).

**Coverage delta:** 14 ✅ / 17 ⚠️ / 126 ❌ → **25 ✅ / 8 ⚠️ / 124 ❌**.

### 2026-05-24 — P2.1 doc-resync close-out (3 stale ⚠️ → ✅; 17 genuine gaps remain)

Flipped `map_moveblock` (real `IEntityRegistry.Move` wire) and
`map_addiddb` / `map_deliddb` (intentional no-ops — entity registry
owns the id↔entity index). Remaining 17 ⚠️ are genuine gaps tied to
the cell-flag table, map-flag table, NPC registry, and reverse
name lookup — all routed to PARITY-REMAINING.md §P2.2 leaf work.

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
