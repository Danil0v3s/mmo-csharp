# MS1 · World data — cells, warps, map list

**Phase:** MS1 (highest priority)
**Depends on:** nothing
**Blocks:** entities, movement, visibility, session

The map server can't do anything until it knows what a "map" is: a grid of cells with walkability flags, plus warp portals connecting maps. This is the bedrock for everything else.

## Source of truth

- [rathena/src/map/mapcache.cpp](/Volumes/1TB/Projetos/rathena/src/map/mapcache.cpp) — binary cell cache format (`mapcache.dat`)
- [rathena/src/map/map.cpp](/Volumes/1TB/Projetos/rathena/src/map/map.cpp) `map_readallmaps`, `map_setcell`, `map_getcell`, `map_addnpc`
- [rathena/src/map/map.hpp](/Volumes/1TB/Projetos/rathena/src/map/map.hpp) — `cell_t`, `map_data`, cell flags
- [rathena/src/tool/mapcache.cpp](/Volumes/1TB/Projetos/rathena/src/tool/mapcache.cpp) — the offline tool that bakes `.gat` files into `mapcache.dat`
- [rathena/db/re/](/Volumes/1TB/Projetos/rathena/db/re/) — `map_index.txt`, `map_cache.dat` (renewal). Pre-renewal data is out of scope.

## Scope (MS1)

**In scope:**
- Load `mapcache.dat` (the rAthena binary format) for the maps the server hosts.
- Per-map cell grid with walkability flags: `WALKABLE`, `NPC`, `WATER`, `CLIFF`, `BASILICA`, `LANDPROTECTOR`, `NOVENDING`, `NOCHAT`, `ICEWALL`, `NOICEWALL`, `NOSKILL`, `MAELSTROM`. Just the static flags from cache; dynamic cells (`map_setcell`) come later.
- `map_getcell(map, x, y, flag)` C# equivalent.
- Warp portal data (read from `npc/warps/*.txt` or equivalent).
- Map name ↔ map id mapping (`map_index.txt`).
- Memory-resident `MapData` per loaded map.

**Out of scope (later phases):**
- `.gat` raw file loading (we use the baked `mapcache.dat` only).
- Dynamic cell flags set by skills (ICEWALL, LANDPROTECTOR, etc.) — added in MS3 skills.
- Map instances / clones (battlegrounds) — separate later phase.
- Map regeneration / weather effects.

## Done

- **`CellFlags` enum + `FromGat(byte)`** ([Map.Server/World/CellFlags.cs](../../../Map.Server/World/CellFlags.cs)) — mirrors rAthena's `map_gat2cell` mapping (gat 0,2,4,6 → walkable+shootable; 1 → blocked; 3 → walkable water; 5 → shootable but not walkable).
- **`MapData`** ([Map.Server/World/MapData.cs](../../../Map.Server/World/MapData.cs)) — per-map cell grid, `GetCell` / `IsWalkable` / `IsShootable` / `IsWater`; out-of-bounds returns `None` (rAthena boundary parity).
- **`MapCacheReader`** ([Map.Server/World/MapCacheReader.cs](../../../Map.Server/World/MapCacheReader.cs)) — binary parser for rAthena's `mapcache.dat`. Handles the 8-byte main header (uint32 file_size + uint16 map_count + 2 bytes C struct alignment padding), the 20-byte per-map info, and ZLib-decompresses the cell payload (`System.IO.Compression.ZLibStream`).
- **`IMapWorldRegistry` + `MapWorldRegistry`** ([Map.Server/World/MapWorldRegistry.cs](../../../Map.Server/World/MapWorldRegistry.cs)) — singleton catalog loaded once at startup; logs warning + skips any configured map missing from the cache.
- **Wired into Map.Server startup** ([Map.Server/Program.cs](../../../Map.Server/Program.cs)) — registered as `IMapWorldRegistry` DI singleton; reads `MapDataPaths` (list) from config.
- **Multi-cache loading** — mirrors rAthena `map.cpp:3798-3802` which searches `db/import → db/re|pre-re → db/map_cache.dat` in order and uses the first cache that contains each map. Our `MapWorldRegistry.Load` walks the configured path list per-map and logs `Loaded map X from Y` so it's auditable. Crucial for renewal targets where `prontera` lives only in the renewal stub cache while `iz_int*` lives only in the full cache. The bug that drove this: pointing at a single cache always missed half the maps; the per-map parse log (added in MapCacheReader.ReadAll) made the gap obvious.
- **`MapServerConfiguration.MapDataPaths`** + appsettings entry (list of strings).
- **`Map.Server.Tests` project created** with 21 tests covering: gat→flags mapping table, MapData boundary behavior, MapCacheReader synthetic round-trips, and smoke tests against the real rAthena renewal cache. Full suite 185 (148 char + 16 login + 21 map).

## Pending — future expansion

Not blocking MS1.entities / session — those can start now against the existing `IMapWorldRegistry`.

1. **Map index / name lookup** — rAthena uses an integer `map_id` internally and a string name for the wire protocol. Parse `map_index.txt`. Required for any packet that carries a map_id.

2. **Warp portals.** Static portal table from rAthena's `npc/warps/*.txt`. Format is simple `mapname,x,y,xs,ys<TAB>warp<TAB>name<TAB>destmap,destx,desty`. [npc.md](npc.md) will share the parser infrastructure.

3. **Cell-based warp lookup**: `MapData.GetWarpAt(x, y)` returns the destination or null. Used by [movement.md](movement.md) when the player walks into a warp cell.

4. **Dynamic cell flags** (MS3) — Ice Wall, Land Protector, Basilica set runtime flags on individual cells. `MapData` stores the base grid immutably; the dynamic layer lives on top (per-cell delta map). Defer until skills system.

### Current file layout (delivered)

```
Map.Server/World/
├── CellFlags.cs             — enum + FromGat() mapping
├── MapData.cs               — per-map cell grid, walkability API
├── IMapWorldRegistry.cs     — interface
├── MapWorldRegistry.cs      — singleton implementation + Load() factory
└── MapCacheReader.cs        — binary parser for mapcache.dat
```

### Future file additions (warps + map index)

```
Map.Server/World/
├── MapIndex.cs              — name ↔ id ↔ MapData
├── Warp.cs                  — Warp record (src map+box, dst map+cell)
└── WarpDb.cs                — parse warp script files, build per-map warp table
```

### Tests (Map.Server.Tests project)

Delivered; 21 passing. Coverage:
- `CellFlagsTests` — gat→flags mapping for all 7 known types + unknown-fallback parity with rAthena.
- `MapDataTests` — constructor validation, out-of-bounds reads return `None`, in-bounds reads match injected cells.
- `MapCacheReaderTests` — header parsing, single + multi-map synthetic round-trips, missing-map returns null, real renewal cache parses cleanly.

Future tests once warps land: `WarpDbTests` for the script parser; `MapDataTests.GetWarpAt`.

## History

- **2026-05-17** — **Multi-cache loading + per-map parse log.** `MapDataPath` (string) → `MapDataPaths` (list). `MapCacheReader.ReadAll` now takes an optional `ILogger` and emits a per-map line during parse (`[i/N] name: xs=… ys=… cells=… compressed=…B`); `MapWorldRegistry.Load` walks the path list and logs `Loaded map X from Y` so it's clear which cache served each map. Validated against the dhxj replay: `prontera` resolved from `db/re/map_cache.dat`, `iz_int03` from `db/map_cache.dat` — the per-map log made this immediately auditable when the user pointed out the cache they used "should" have `iz_int03". Mirrors rAthena `map.cpp:3845-3848`.
- **2026-05-16** — **MS1.world cell loading shipped.** Implemented `CellFlags`, `MapData`, `MapCacheReader`, `IMapWorldRegistry` + `MapWorldRegistry`. Wired into `Map.Server/Program.cs` startup via DI singleton reading `MapDataPath` from config. Created `Map.Server.Tests` project with 21 tests; full suite now 185 green. Real rAthena `db/re/map_cache.dat` parses cleanly. Important wrinkle discovered during implementation: C struct `main_header { uint32; uint16; }` is `sizeof = 8`, not 6 — the trailing 2-byte alignment padding is part of the on-disk format. Documented in code comments. Warp portals + map_index.txt parsing deferred to next iteration; not blocking MS1.entities/session.
- **2026-05-16** — Plan written. No implementation yet.
