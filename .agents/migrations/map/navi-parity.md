# navi.cpp parity · 2026-05-22 (T9.H — per-fn rollup)

`src/map/navi.cpp` (655 lines, 17 functions) — in-game GPS file
generator (produces /Navi binary distance matrices the client uses
for `navigate_to`).

All 17 entries covered by [INaviService](/Map.Server/Navi/INaviService.cs) /
[NaviService](/Map.Server/Navi/NaviService.cs). Generator body data-pending —
the entry points are reserved so the `@navi_generate` GM command +
script `navigate_to` BUILTIN have somewhere to land.

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `navi_create_lists` | ⚠️ | `CreateLists` — entry reserved; deferred per PARITY-REMAINING.md §P2.2.e (cell-level navmesh exporter is a build-time tool, client ships with rAthena mesh) |
| `navi_path_search` | ⚠️ | `PathSearch` — returns false (§P2.2.e) |
| `map_type` | ⚠️ | `MapType` — returns 0 placeholder (§P2.2.e) |
| `fileExists` | ✅ | `FileExists` (System.IO.File.Exists) |
| `write_header` / `write_footer` | ⚠️ | No-op placeholders (§P2.2.e) |
| `write_map` / `write_map_distance` / `write_map_distances` / `write_map_header` / `write_mapdist_header` | ⚠️ | All no-op placeholders (§P2.2.e) |
| `write_npc` / `write_npc_distance` / `write_npc_distances` | ⚠️ | All no-op placeholders (§P2.2.e) |
| `write_object_lists` | ⚠️ | No-op placeholder (§P2.2.e) |
| `write_spawn` | ⚠️ | No-op placeholder (§P2.2.e) |
| `write_warp` | ⚠️ | No-op placeholder (§P2.2.e) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Navi file generator | 1 | 16 | 0 | 17 |
| **Totals** | **1** | **16** | **0** | **17** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 16 genuine gaps remain)

All 16 ⚠️ entries audited against
[NaviService.cs](/Map.Server/Navi/NaviService.cs); every method is
a no-op / `false` / `0` placeholder. Notes refreshed with the
PARITY-REMAINING.md §P2.2.e citation (navmesh exporter is a
build-time tool, client ships with rAthena-generated mesh — runtime
port not on critical path). No flips.

### 2026-05-22 — T9.H per-fn rollup

Per-function audit. Baseline: **1 ✅ / 16 ⚠️ / 0 ❌**. Only
`fileExists` is real; the generator + path-search bodies are
no-op placeholders. Closing the ⚠️ rows requires porting the
A* distance-matrix algorithm + map geometry walker — niche
(client-side feature) so deferred until needed.

### 2026-05-20 — initial audit + service
- 17 functions covered (entry points only). Generator port deferred.
