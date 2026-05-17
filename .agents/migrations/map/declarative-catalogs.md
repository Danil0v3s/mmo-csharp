# Declarative map catalogs — warps, mob spawns, map flags

**Phase:** MS1 (world / movement / visibility) + MS2 (spawn)
**Depends on:** [world.md](world.md) for map names; nothing else
**Used by:** [movement.md](movement.md) (warp pickup on walk), [spawn.md](spawn.md) (mob population), MS3 mapflag-gated systems (PvP/GvG/restricted/nopvp/…)

rAthena ships a large body of declarative content in `npc/re/`:
warp portals, mob spawn points, and per-map flags. None of these
need the script engine — they're flat records. This doc covers
how those records are stored in our DB and how the parser pipeline
keeps them in sync with rAthena.

## What's in scope

Three rAthena entry-point conf files are walked:

| Conf | What it lists | Lines we care about |
|---|---|---|
| `npc/re/scripts_warps.conf` | warp-portal files | `srcmap,x,y,dir <TAB> warp <TAB> name <TAB> xs,ys,destmap,destx,desty` |
| `npc/re/scripts_monsters.conf` | mob-spawn files | `map,x,y[,xs,ys] <TAB> monster|boss_monster <TAB> name <TAB> mob_id,amount,delay1,delay2[,event[,size[,ai]]]` |
| `npc/re/scripts_mapflags.conf` | mapflag files | `mapname <TAB> mapflag <TAB> flag [<TAB> value]` |

**Out of scope** (deferred until the script engine ports):

- Scripted warps (`WARPNPC` with a `{ ... }` body that branches on quest state, party size, etc.). Lives in `npc/re/warps/…` alongside the declarative warps; the parser skips any line whose directive isn't `warp`/`warp2`.
- NPC bodies (cities, kafras, guides, merchants, jobs, quests, custom, instances). Everything under `npc/re/cities/`, `npc/re/guides/`, etc. is script-bodied.
- Achievement, reputation, exp-curve, job-base-hp DBs — those use the YAML loader rAthena has on top of these flat files.

## Database tables

Three new tables, all in `Core.Database`:

### `warp`

| Column | Type | Notes |
|---|---|---|
| `warp_id` | int PK auto | |
| `src_map` | varchar(24) | indexed |
| `src_x`, `src_y` | smallint | center cell of the trigger area |
| `src_dir` | tinyint unsigned | facing direction, usually 0 |
| `warp_type` | varchar(8) | `warp` or `warp2` |
| `name` | varchar(64) | script-unique identifier per srcmap |
| `span_xs`, `span_ys` | smallint | trigger half-extent |
| `dst_map` | varchar(24) | |
| `dst_x`, `dst_y` | smallint | |

Index: `(src_map, src_x, src_y)` — the map server's natural lookup.

### `mob_spawn`

| Column | Type | Notes |
|---|---|---|
| `spawn_id` | int PK auto | |
| `map_name` | varchar(24) | indexed |
| `center_x`, `center_y` | smallint | 0/0 with empty span = "anywhere on map" |
| `span_xs`, `span_ys` | smallint | half-extent of spawn area |
| `is_boss` | bool | `boss_monster` vs `monster` |
| `display_name` | varchar(64) | empty = use mob_db default |
| `mob_id` | int | foreign-key-shape to `mob_db.id` (not enforced) |
| `amount` | int | how many mobs to keep alive |
| `delay1`, `delay2` | int | respawn timer base + variance, ms |
| `event_label` | varchar(64) | optional OnDeath event |
| `size` | int | rAthena 0/1/2 override |
| `ai` | int | rAthena AI mode override |

Index: `map_name`.

### `map_flag`

| Column | Type | Notes |
|---|---|---|
| `flag_id` | int PK auto | |
| `map_name` | varchar(24) | indexed |
| `flag` | varchar(32) | `pvp`, `gvg`, `restricted`, `nosave`, … |
| `value` | varchar(128) | empty for toggles, `off` for opt-outs, number for tiered flags, or comma-list for `nosave` |

The same flag can be set multiple times for a map (rAthena permits `pvp` + `pvp_noparty` + `pvp_nightmaredrop`, etc.) — `(map_name, flag)` is **not** unique. Index on `map_name` only.

## Entities and EF configurations

- [WarpEntity.cs](../../../Core.Database/Entities/WarpEntity.cs) + [WarpEntityConfiguration.cs](../../../Core.Database/Configurations/WarpEntityConfiguration.cs)
- [MobSpawnEntity.cs](../../../Core.Database/Entities/MobSpawnEntity.cs) + [MobSpawnEntityConfiguration.cs](../../../Core.Database/Configurations/MobSpawnEntityConfiguration.cs)
- [MapFlagEntity.cs](../../../Core.Database/Entities/MapFlagEntity.cs) + [MapFlagEntityConfiguration.cs](../../../Core.Database/Configurations/MapFlagEntityConfiguration.cs)

`GameDbContext` exposes `DbSet<WarpEntity> Warps`, `DbSet<MobSpawnEntity> MobSpawns`, `DbSet<MapFlagEntity> MapFlags`.

Migration: `20260517115806_AddDeclarativeMapCatalogs` (3 tables + 3 indexes).

## Parser / importer

[Tools.RathenaImporter/](../../../Tools.RathenaImporter/) is a console app that reads the three entry-point conf files, walks every referenced `.txt`, parses one row per line, and emits batched-INSERT SQL into a target directory.

```
dotnet run --project Tools.RathenaImporter -- <rathena-repo-path> <output-dir>
```

Output files:
- `seed_warps.sql`
- `seed_mob_spawns.sql`
- `seed_map_flags.sql`

Each script starts with `DELETE FROM <table>;` so re-running idempotently replaces the catalog.

The parser **silently drops** lines that aren't pure declarative — anything with a `script` directive, comment-only lines, malformed lines, or invalid coordinate formats. Files with mixed script + warp content (`npc/re/warps/cities/prontera.txt` has both) extract only the declarative entries.

## Current rAthena snapshot counts

Parsed from `/Volumes/1TB/Projetos/rathena` on 2026-05-17:

| Table | Rows | Source files |
|---|---|---|
| `warp` | 1,279 | 56 files under `npc/re/warps/` |
| `mob_spawn` | 2,950 | 100 files under `npc/re/mobs/` |
| `map_flag` | 2,251 | 21 files under `npc/re/mapflag/` |

Seed files committed to [Core.Database/Seeds/Scripts/](../../../Core.Database/Seeds/Scripts/) and wired into `DatabaseSeeder`.

## Refresh workflow

When rAthena's source changes (or you point at a different fork):

1. Re-run the importer: `dotnet run --project Tools.RathenaImporter -- <repo> /tmp/seeds`
2. Diff the output against `Core.Database/Seeds/Scripts/` and copy over.
3. Either re-run `DatabaseSeeder.SeedAsync()` at server boot, or apply manually:
   `docker exec -i rathena_db mysql -uragnarok -pragnarok ragnarok < Core.Database/Seeds/Scripts/seed_warps.sql`

## What still needs to be done

The tables and the seed data exist; runtime consumption does not yet:

- **Map server warp lookup.** `IMapWorldRegistry` knows about cells; nothing yet checks "did the player walk onto a warp?" The hook point is [`MovementService`](../../../Map.Server/Movement/MovementService.cs) on cell-arrival → query `warp` table by `(map, x, y)` → call `pc_setpos` equivalent. Plan: add a `WarpService` loaded once at boot per `IMapWorldRegistry.All` map; cache in-memory.
- **Mob spawn manager.** [`IMobSpawnService`](../../../Map.Server/Spawn/) already exists but is hardcoded; needs to load from `mob_spawn` at startup and respect `(span_xs, span_ys, delay1, delay2)` per row.
- **Map flag application.** No code yet reads `map_flag`. When MS3 PvP/restricted/town-mode systems land, `MapData` gains a `Flags` dictionary populated from `map_flag` rows at startup.

## History

- **2026-05-17** — Initial port. Tables + EF entities + migration + importer tool + first seed snapshot (1279 warps / 2950 mob spawns / 2251 map flags). No runtime consumers yet.
