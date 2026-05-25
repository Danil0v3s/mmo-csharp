# navi.cpp parity · 2026-05-25 (wave 75 — close-out)

`src/map/navi.cpp` (655 lines, 17 functions) — in-game GPS file
generator (produces /Navi binary distance matrices the client uses
for `navigate_to`).

**Key finding (wave 75):** every `navi_*` callsite in rAthena
is gated behind `#ifdef MAP_GENERATOR` (see
[rathena/src/map/map.cpp:5333-5343](/Volumes/1TB/Projetos/rathena/src/map/map.cpp)).
The runtime map-server binary never invokes `navi_create_lists` /
`navi_path_search` / the `write_*` family — these only execute when
rAthena is compiled as the build-time `map-server-generator` CLI
that emits client navmesh blobs. The C# port intentionally collapses
this to a stub service: the interface exists so the GM-command tier
(`@navi_generate`) and script BUILTIN `navigate_to` have something
to invoke, but the runtime no-op is by design and matches rAthena
behavior when `MAP_GENERATOR` is undefined.

All 17 entries covered by [INaviService](/Map.Server/Navi/INaviService.cs) /
[NaviService](/Map.Server/Navi/NaviService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `navi_create_lists` | ✅ | [`NaviService.CreateLists`](/Map.Server/Navi/NaviService.cs):15 — runtime no-op (returns false); rAthena callsite gated `#ifdef MAP_GENERATOR` (map.cpp:5337) so this method is never reached at runtime. PARITY-REMAINING.md §P2.2.e documents the build-time tool path. |
| `navi_path_search` | ✅ | [`NaviService.PathSearch`](/Map.Server/Navi/NaviService.cs):25 — runtime no-op (returns false); only invoked by `navi_create_lists` (recursive build-time walker, navi.cpp:325/535/606) which itself never runs at runtime. (§P2.2.e) |
| `map_type` | ✅ | [`NaviService.MapType`](/Map.Server/Navi/NaviService.cs):26 — returns 0; consumed only by build-time `navi_create_lists` to bucket cells. (§P2.2.e) |
| `fileExists` | ✅ | [`NaviService.FileExists`](/Map.Server/Navi/NaviService.cs):27 — `System.IO.File.Exists` (real impl) |
| `write_header` | ✅ | [`NaviService.WriteHeader`](/Map.Server/Navi/NaviService.cs):29 — runtime no-op; build-time generator helper. (§P2.2.e) |
| `write_footer` | ✅ | [`NaviService.WriteFooter`](/Map.Server/Navi/NaviService.cs):30 — runtime no-op; build-time generator helper. (§P2.2.e) |
| `write_map_header` | ✅ | [`NaviService.WriteMapHeader`](/Map.Server/Navi/NaviService.cs):31 — runtime no-op. (§P2.2.e) |
| `write_map` | ✅ | [`NaviService.WriteMap`](/Map.Server/Navi/NaviService.cs):32 — runtime no-op. (§P2.2.e) |
| `write_map_distance` | ✅ | [`NaviService.WriteMapDistance`](/Map.Server/Navi/NaviService.cs):33 — runtime no-op. (§P2.2.e) |
| `write_map_distances` | ✅ | [`NaviService.WriteMapDistances`](/Map.Server/Navi/NaviService.cs):34 — runtime no-op. (§P2.2.e) |
| `write_mapdist_header` | ✅ | [`NaviService.WriteMapDistHeader`](/Map.Server/Navi/NaviService.cs):35 — runtime no-op. (§P2.2.e) |
| `write_npc` | ✅ | [`NaviService.WriteNpc`](/Map.Server/Navi/NaviService.cs):36 — runtime no-op. (§P2.2.e) |
| `write_npc_distance` | ✅ | [`NaviService.WriteNpcDistance`](/Map.Server/Navi/NaviService.cs):37 — runtime no-op. (§P2.2.e) |
| `write_npc_distances` | ✅ | [`NaviService.WriteNpcDistances`](/Map.Server/Navi/NaviService.cs):38 — runtime no-op. (§P2.2.e) |
| `write_object_lists` | ✅ | [`NaviService.WriteObjectLists`](/Map.Server/Navi/NaviService.cs):39 — runtime no-op. (§P2.2.e) |
| `write_spawn` | ✅ | [`NaviService.WriteSpawn`](/Map.Server/Navi/NaviService.cs):40 — runtime no-op. (§P2.2.e) |
| `write_warp` | ✅ | [`NaviService.WriteWarp`](/Map.Server/Navi/NaviService.cs):41 — runtime no-op. (§P2.2.e) |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Navi file generator | 17 | 0 | 0 | 17 |
| **Totals** | **17** | **0** | **0** | **17** |

## History

### 2026-05-25 — Wave 75: navi-parity close-out (16 ⚠️ → ✅)

Re-audited all 16 ⚠️ rows. Discovered the entire `navi_*` family is
gated behind `#ifdef MAP_GENERATOR` in rAthena
([map.cpp:5333-5343](/Volumes/1TB/Projetos/rathena/src/map/map.cpp)) —
the runtime map-server binary never invokes any of these functions.
They only execute when rAthena is compiled as the standalone
`map-server-generator` CLI tool, which emits client navmesh blobs
at build time. The C# port collapses this entire surface to a stub
service intentionally, matching rAthena's runtime behavior when
`MAP_GENERATOR` is undefined (i.e. every shipped server build).

Per the wave-75 rubric ("Intentionally absent → promote ⚠️/❌ → ✅
with rationale"), all 16 ⚠️ rows flip ✅ with the
`#ifdef MAP_GENERATOR` gate + PARITY-REMAINING.md §P2.2.e citation.
If the C# project ever wants the build-time mesh exporter, it
would live as a separate console-mode tool, not as runtime
service code. No C# code touched in this pass.

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
