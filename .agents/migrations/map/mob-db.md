# MS2 · Mob database

**Phase:** MS2
**Depends on:** [entities.md](entities.md) (for the runtime `MobEntity`), `Core.Database.Repositories.Api.IMobRepository`
**Blocks:** [spawn.md](spawn.md), all MS3 combat

The mob db is the static catalog of monster definitions: stats, sprite ids, drops, skill assignments. Independent of any particular spawn instance. We load it from the `mob_db` table via `IMobRepository`, mirroring rAthena's optional `use_sql_db: yes` mode in `inter_athena.conf` (the `mob_read_sqldb` path in `mob.cpp`). The shipped seed scripts under [Core.Database/Seeds/Scripts/seed_mob_db.sql](../../../Core.Database/Seeds/Scripts/seed_mob_db.sql) are a column-for-column match for rAthena's renewal `mob_db_re` table.

## Source of truth

- [rathena/src/map/mob.hpp](/Volumes/1TB/Projetos/rathena/src/map/mob.hpp) — `struct s_mob_db`
- [rathena/src/map/mob.cpp](/Volumes/1TB/Projetos/rathena/src/map/mob.cpp) — `mob_read_db` (YAML), **`mob_read_sqldb`** (SQL alternate path, lines 5198–5450), `mobdb_search_aegisname`
- [rathena/conf/inter_athena.conf](/Volumes/1TB/Projetos/rathena/conf/inter_athena.conf) — `use_sql_db: yes/no` flag and `mob_table` / `item_table` overrides
- [Core.Database/Entities/MobEntity.cs](../../../Core.Database/Entities/MobEntity.cs) — the persisted schema (inline `Drop1Item..Drop8Item` columns)
- [Core.Database/Seeds/Scripts/seed_mob_db.sql](../../../Core.Database/Seeds/Scripts/seed_mob_db.sql) — renewal seed (2554 mobs)

## Scope (MS2)

**In scope:**
- In-memory catalog `IMobDb` hydrated once at map-server startup from `IMobRepository.GetAllAsync()`.
- Indexes keyed by mob class id (the integer mob class) AND aegis name (the string id, e.g. `PORING`).
- `MobDbEntry` shape: stats, sprite ids, ranges, walk/attack timing, drops flattened from the 8 inline drop columns into a list, race-groups / modes surfaced from the bit columns.
- Drop table stored as `IReadOnlyList<MobDrop>`; combat death emission lives with [adjacent/combat.md](adjacent/combat.md).
- Mob skill block: stored opaquely for MS3 skills to consume.

**Out of scope:**
- Combat-related fields beyond storage: ATK/MATK, defense, MVP rules — stored but unused in MS2.
- Mob skill execution — MS3.
- Per-map mob modifiers (e.g. `mob_avail` sprite swaps) — later.
- Live edit / `/reloadmobdb` — wired but expects DB roundtrip; rAthena's CSV/YAML reload path is replaced by re-querying `IMobRepository`.

## Done

- [`MobDbEntry`](../../../Map.Server/MobDb/MobDbEntry.cs) + [`MobDrop`](../../../Map.Server/MobDb/MobDbEntry.cs) records — mob_db field surface.
- [`IMobDb`](../../../Map.Server/MobDb/IMobDb.cs) + [`MobDb`](../../../Map.Server/MobDb/MobDb.cs) — singleton, id + aegis-name indexes, `Reload()` swaps an immutable snapshot.
- Namespace is `Map.Server.Mob` (not `Map.Server.MobDb`) so tests inside the same project don't get a class-vs-namespace collision on the bare name `MobDb`.

## Pending

### Items, in order

1. **DB-backed loader.** Replace the file-reader implementation with one that pulls from `IMobRepository.GetAllAsync()` at construction. Drop the YamlDotNet dependency and the `MobDbYamlReader` class. Tests use repository stubs.

2. **`MobDbEntry` field set.** Map the rAthena `MobEntity` columns from `Core.Database.Entities.MobEntity` (uint Id, NameAegis, NameEnglish, NameJapanese, Level, Hp, BaseExp, etc.). Stats use the rAthena defaults when nullable.

3. **Drops flattening.** The DB has 8 fixed slots (`Drop1Item`/`Drop1Rate`...`Drop8Item`/`Drop8Rate`) plus `Drop[1-8]Nosteal` / `Drop[1-8]Option` / `Drop[1-8]Index`. The loader walks the 8 slots and emits a `List<MobDrop>` per entry; null `DropNItem` ends the list.

4. **MVP drop slots.** rAthena's `MobEntity` has `MvpDrop1..3Item` + `Rate` + `RandomOptionGroup`. Surface as `MvpDrops` parallel to `Drops`.

5. **Race-groups + modes as a bool dictionary.** rAthena's flat columns (`RacegroupGoblin`, `RacegroupKobold`, `ModeAggressive`, etc.) map back into a `IReadOnlyDictionary<string, bool>` keyed by the rAthena flag name. Keeps the existing API; only the source changes.

6. **Aegis-name index.** Spawn config and scripts reference mobs by aegis name; the network protocol uses class id. Build both indexes at load. Case-insensitive on the aegis side.

7. **`Reload()` requeries the DB.** Snapshot swap stays — atomic read/write semantics unchanged. Cost: one round-trip to the DB (`GetAllAsync()`).

### File layout

```
Map.Server/MobDb/
├── MobDbEntry.cs
├── IMobDb.cs
└── MobDb.cs                — singleton, hydrates from IMobRepository
```

(`MobDbYamlReader.cs` removed.)

### Tests

1. `MobDbTests` with a stub `IMobRepository` that returns a synthetic list:
   - Lookup by id and aegis-name.
   - Drops list is built only from the populated slots (empty when `Drop1Item == null`).
   - Snapshot replacement on `Reload()`.
2. Smoke: load real DB at integration-test time (defer until we have a test DB harness; the per-mob field shape is exercised by unit tests).

### Acceptance

- `IMobDb` loads all 2554 mobs from the seeded `mob_db` table at startup in under 1 second.
- `Get(1002)` returns Poring with level 1, hp 55, drops including `Jellopy` at rate 7000.
- `GetByAegisName("PORING")` returns the same entry.

## History

- **2026-05-16** — Plan written. No implementation yet.
- **2026-05-16** — YAML reader + singleton shipped. 2555-mob renewal db loads in < 200 ms.
- **2026-05-16** — Switched plan to DB-backed (`Core.Database.Entities.MobEntity` + `IMobRepository`). The seeded SQL schema is rAthena's `use_sql_db` mode (column-for-column parity with `mob_db_re`). The YAML reader and YamlDotNet dependency will be removed in the refactor commit; the `IMobDb` / `MobDbEntry` API surface stays.
