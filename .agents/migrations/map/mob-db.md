# MS2 · Mob database

**Phase:** MS2
**Depends on:** [entities.md](entities.md) (for `MobEntity`)
**Blocks:** [spawn.md](spawn.md), all MS3 combat

The mob db is the static catalog of monster definitions: stats, sprite ids, drops, skill assignments. Independent of any particular spawn instance. rAthena parses YAML files; we'll do the same to minimize data drift.

## Source of truth

- [rathena/src/map/mob.hpp](/Volumes/1TB/Projetos/rathena/src/map/mob.hpp) — `struct s_mob_db`
- [rathena/src/map/mob.cpp](/Volumes/1TB/Projetos/rathena/src/map/mob.cpp) — `mob_read_db`, `mob_parse_dbrow` (YAML loader), `mobdb_search_aegisname`
- [rathena/db/re/mob_db.yml](/Volumes/1TB/Projetos/rathena/db/re/mob_db.yml) — the actual data (3500+ mobs in renewal)
- [rathena/db/re/mob_db2.yml](/Volumes/1TB/Projetos/rathena/db/re/mob_db2.yml) — extension/override
- [rathena/db/re/mob_skill_db.yml](/Volumes/1TB/Projetos/rathena/db/re/mob_skill_db.yml) — per-mob skill assignment (defer to MS3 skills)

## Scope (MS2)

**In scope:**
- Parser for `mob_db.yml` + `mob_db2.yml` overrides.
- In-memory catalog `IMobDb` keyed by mob class id (the integer mob class) AND aegis name (the string id, e.g. `PORING`).
- Fields needed for MS2 spawn + display: `Id`, `AegisName`, `Name`, `JapaneseName`, `Level`, `Hp`, `Sp`, `BaseExp`, `JobExp`, `MvpExp`, `AttackRange`, `Size`, `Race`, `Element`, `Mode`, `MoveSpeed`, `AttackDelay`, `AttackMotion`, `DamageMotion`, `WalkMask`, `View` (sprite info: class, hair, weapon, head — for some mob "monsters" that are humanoid).
- Drop table: just store it; actual drop emission is MS3 items.
- Mob skill block: stored opaquely as `RawSkillEntries` for MS3 skills to interpret.

**Out of scope:**
- Combat-related fields beyond storage: ATK/MATK, defense, MVP rules — stored but unused in MS2.
- Mob skill execution — MS3.
- Per-map mob modifiers (e.g. `mob_avail.yml` sprite swaps) — phase later.

## Done

Nothing. There's no mob system in `Map.Server`.

## Pending

### Items, in order

1. **YAML reader.** Add `YamlDotNet` package to Map.Server (or a new `Core.Data` project). Parse rAthena's mob YAML format. The schema is well-documented in [rathena/doc/](/Volumes/1TB/Projetos/rathena/doc/) but the shape is large; expect a 200-line schema.

2. **`MobDbEntry`** record with the fields listed above. Use nullable refs / defaults to handle missing values (rAthena defaults many fields).

3. **`IMobDb` singleton:**
   - `MobDbEntry? Get(int classId)`
   - `MobDbEntry? GetByAegisName(string aegisName)`
   - `IEnumerable<MobDbEntry> All()`
   - `void Reload()` — for GM `/reloadmobdb` later.

4. **Two-file loader.** Load `mob_db.yml` first; then `mob_db2.yml` overrides existing entries. Match rAthena's behavior: `mob_db2.yml` entries with the same id replace; new ids are added.

5. **Data path config.** Add `MobDbPath` and `MobDbOverridePath` to `MapServerConfiguration`. Default to rAthena's `/Volumes/1TB/Projetos/rathena/db/re/mob_db.yml` and `mob_db2.yml`.

6. **Aegis-name lookup index.** Spawn config and scripts reference mobs by aegis name (`PORING`); the network protocol uses class id (`1002`). Build both indexes at load.

7. **View data.** Some "mobs" are humanoid (e.g. `MARSE` in PvP). The View block in mob_db carries hair/weapon/head ids. Store opaquely; visibility ([visibility.md](visibility.md)) will emit different `ZC_NOTIFY_STANDENTRY` payload for humanoid-view mobs.

### File layout

```
Map.Server/MobDb/
├── MobDbEntry.cs
├── IMobDb.cs
├── MobDb.cs                  — singleton, loads from YAML
├── MobDbYamlReader.cs        — schema parser
└── MobMode.cs                — flags enum mirroring rAthena's mode field
```

### Tests

1. `MobDbYamlReaderTests`:
   - Parse a 3-mob synthetic YAML; assert fields match.
   - Override file replaces by id.
   - Malformed YAML → throws with a useful error.
2. Smoke: load real `mob_db.yml`. Assert at least one well-known mob (`PORING` / id 1002) has expected level (1), hp (50), and aegis name.

### Acceptance

- `IMobDb` loads all ~3500 mobs from rAthena's renewal db at startup in under 1 second.
- `Get(1002)` returns Poring with level 1, hp 50.
- `GetByAegisName("PORING")` returns the same entry.

## History

- **2026-05-16** — Plan written. No implementation yet.
