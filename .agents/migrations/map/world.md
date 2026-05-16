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
- [rathena/db/](/Volumes/1TB/Projetos/rathena/db/) — `map_index.txt`, `re/map_cache.dat`, `pre-re/map_cache.dat`

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

Nothing yet. The `Map.Server` project has no map-loading code.

## Pending

### Items, in suggested order

1. **Decide on map data source format.** Two options:
   - **(A) Reuse rAthena's `mapcache.dat`** binary file. Pros: zero data conversion, ships with every rAthena server. Cons: opaque binary; need a faithful parser.
   - **(B) Bake to a more C#-friendly format** at startup (e.g. one `.gz` per map with width/height + cell bitmap). Pros: easier to read. Cons: requires a converter tool.

   Recommendation: **option A**. The format is small and well-documented. Write the parser once, done.

2. **Locate `mapcache.dat`** — rAthena keeps it at `/Volumes/1TB/Projetos/rathena/db/{re,pre-re}/map_cache.dat`. Pin the renewal/pre-renewal choice via config (`appsettings.json` already has `Maps` array; add `RenewalMode: re|pre-re`).

3. **Port the mapcache binary parser** to `Core.Database` or a new `Map.Server/World/` namespace. The file layout (per rAthena `mapcache.cpp`):
   ```
   header { uint32 version; uint32 file_size; uint16 map_count; }
   for each map {
     char name[12]; int16 xs; int16 ys; int32 len;
     byte[len] cells // RLE-compressed walkability byte per cell
   }
   ```
   Decompress with rAthena's encoding (run-length on consecutive identical cells).

4. **`MapData` class** with the cell grid + size + a `CellFlags` enum. Fast `GetCell(x, y)` and `IsWalkable(x, y)`. Public read-only API; mutation lives behind `SetCell(x, y, flag, set)` for MS3 dynamic cells.

5. **`IMapDataRegistry`** singleton: load all configured maps at startup, hand out `MapData` by name. The existing `IMapServerRegistryService` in **Char.Server** is a different thing (registers map servers with the char server) — name this differently. Suggested: `IMapWorldRegistry` in `Map.Server.World`.

6. **Map index / name lookup.** rAthena uses an integer `map_id` internally and a string name for the wire protocol. Both need to round-trip. Parse `map_index.txt` (one map name per line; line number is map_id starting at 1).

7. **Warp portals.** rAthena loads warps from script files (`npc/warps/*.txt`). For MS1 we only need the static portal table: source map + cell range + destination map + dest cell. Two options:
   - **(A)** Port the warp-portion of rAthena's script parser (minimal — warps are very simple `mapname,x,y,xs,ys<TAB>warp<TAB>name<TAB>destmap,destx,desty` syntax).
   - **(B)** Bake warps into a JSON / SQL table for our convenience.

   Recommendation: **option A** — the format is tiny and we'll need NPC/mob parsing later anyway; share infrastructure.

8. **Cell-based warp lookup**: `MapData.GetWarpAt(x, y)` returns the destination or null. Used by [movement.md](movement.md) when the player walks into a warp cell.

9. **Renewal vs pre-renewal**: pin via config. rAthena ships two map_cache files; pick one at startup, load it, log which mode is active.

### File layout

```
Map.Server/World/
├── MapData.cs               — per-map cell grid, walkability API
├── CellFlags.cs             — enum of static cell flags
├── IMapWorldRegistry.cs     — interface
├── MapWorldRegistry.cs      — singleton implementation
├── MapCacheReader.cs        — binary parser for mapcache.dat
├── MapIndex.cs              — name ↔ id ↔ MapData
├── Warp.cs                  — Warp record (src map+box, dst map+cell)
├── WarpDb.cs                — parse warp script files, build per-map warp table
└── WorldConfiguration.cs    — RenewalMode, paths to map_cache + map_index + warps
```

### Tests (Map.Server.Tests project — to be created)

1. `MapCacheReaderTests` — round-trip a small synthetic mapcache buffer; verify per-cell walkability.
2. `MapDataTests` — `GetCell` boundary cases (negative coords, beyond xs/ys), walkability returns false outside bounds.
3. `WarpDbTests` — parse a few synthetic warp script lines; lookup by cell returns the destination; non-warp cell returns null.
4. Smoke: load the real `prontera` from rAthena's renewal `map_cache.dat`, verify cell at `(156, 191)` is walkable (well-known prontera spawn).

### Open decisions

- **Renewal vs pre-renewal**: the C# stack hasn't picked one. Default to renewal (rAthena's `re/`). Config knob to override.
- **Where to physically locate the data files**: bind via config to rAthena's `/db/re/` so we don't duplicate the data. Map.Server's `appsettings.json` already has `Maps` array; add `MapDataPath: "/Volumes/1TB/Projetos/rathena/db/re/map_cache.dat"`.
- **Path resolution for warp scripts**: rAthena scatters them across `npc/warps/{city,fields,dungeons,...}/*.txt`. We'll need a recursive scanner; pick a `WarpScriptRoot` config.

### Acceptance

- `MapWorldRegistry` loads the configured map list (`prontera`, `new_1-1`, `lasa_fild01` per current appsettings) from `map_cache.dat`.
- Each loaded map exposes its cell grid; `IsWalkable(x, y)` returns the expected value for sampled coordinates.
- Warp lookup works for at least one known warp (e.g. `prontera (273, 354)` → `prt_fild05 (170, 32)`).
- Startup log shows map count, total cell count, renewal mode.

## History

- **2026-05-16** — Plan written. No implementation yet.
