# MS2 · Mob spawn manager

**Phase:** MS2
**Depends on:** [mob-db.md](mob-db.md), [world.md](world.md), [entities.md](entities.md), [npc.md](npc.md) (`monster` builtin contributes spawn entries)
**Blocks:** anything that depends on living mobs being on the map (combat in MS3)

The spawn manager owns the *instances* of mobs on each map: where they live, how many, when they respawn after death. Static catalog of "what is a Poring" is the mob_db; this doc is about "how many Porings are on payon_fild01, where do they walk, and when do they come back."

## Source of truth

- [rathena/src/map/mob.cpp](/Volumes/1TB/Projetos/rathena/src/map/mob.cpp) — `mob_parse_dataset`, `mob_spawn`, `mob_setdelayspawn`, `mob_dead` / `mob_revive`
- [rathena/db/re/mob_avail.yml](/Volumes/1TB/Projetos/rathena/db/re/mob_avail.yml) — alternate-sprite overrides
- [rathena/npc/re/mobs/](/Volumes/1TB/Projetos/rathena/npc/re/mobs/) — per-area `.txt` spawn configs (script `monster` lines)

rAthena unifies "scripted mob spawn lines" with `monster` script command. We do the same: spawn config is just a side-effect of NPC parsing.

## Scope (MS2)

**In scope:**
- Per-map spawn config: list of `(mobClass, area, minAmount, respawnDelay)` entries.
- Initial spawn pass on map load: instantiate `MobEntity` for each entry, place at a random walkable cell in the configured area.
- Respawn timer: when a mob entity dies (admin command for MS2; combat for MS3), schedule respawn after the configured delay.
- Random idle wander: mobs pick a random walkable destination every `mob_db.MoveSpeed * ~10` and walk there. Reuses [movement.md](movement.md) walk loop.
- `ZC_NOTIFY_STANDENTRY` for newly spawned mobs (visibility).
- Death cleanup: remove mob from registry, broadcast `ZC_NOTIFY_VANISH`, schedule respawn.

**Out of scope (MS3):**
- Aggro / chase / attack — combat doc.
- Mob skills (`mob_skill_db`) — skills doc.
- Boss / mvp respawn announcements.
- Treasure boxes (TYPE_TREASURE).
- Mob-vs-mob aggression (mass mobs).

## Done

- [`MobSpawnEntry`](../../../Map.Server/Spawn/MobSpawnEntry.cs) — static declaration of one spawn slot (class, map, box, amount, respawn delay+jitter).
- [`MobEntity`](../../../Map.Server/Entities/MobEntity.cs) upgraded with a `MobDbEntry` reference, current Hp/Sp, the `Origin` spawn entry, and `NextWanderTick`. Speed mirrors `mob_db.WalkSpeed`.
- [`IMobSpawnRegistry`](../../../Map.Server/Spawn/IMobSpawnRegistry.cs) + [`MobSpawnRegistry`](../../../Map.Server/Spawn/MobSpawnRegistry.cs) — concurrency-safe collector indexed by mapId. NPC parser plugs into this in [npc.md](npc.md).
- **DB source of truth**: 2,950 spawn entries seeded from rAthena `npc/re/mobs/` into the `mob_spawn` table — see [declarative-catalogs.md](declarative-catalogs.md). The spawn service still hardcodes its fixtures; loading from the DB is the next concrete step.
- [`IMobSpawnService`](../../../Map.Server/Spawn/IMobSpawnService.cs) + [`MobSpawnService`](../../../Map.Server/Spawn/MobSpawnService.cs):
  - `SpawnInitial()` — places mobs per entry, broadcasts STANDENTRY, called once from `MapServerImpl.StartAsync`.
  - `Tick()` — per-tick wander + respawn promotion; called from `UpdateGameLogicAsync`.
  - `KillMob(EntityId)` — death entry point: VANISH broadcast, registry removal, respawn scheduled per entry (delay + random jitter).
- Visibility's `BuildStandEntry` now emits a mob-shaped `ZC_NOTIFY_STANDENTRY` (objecttype=5, Job=classId, name from mob_db / spawn override).
- 9 tests in [Map.Server.Tests/Spawn/](../../../Map.Server.Tests/Spawn/) covering amount/box placement, broadcasts, unknown-class skip, kill flow, zero-delay respawn, and wander start.

### Pending (deferred)

- Spawn entries are populated programmatically today; the NPC parser feeds them automatically once [npc.md](npc.md) lands.
- GM `@killmob` packet handler — `MobSpawnService.KillMob` is the entry point but no client/admin packet calls it yet.
- Wander algorithm is the simple "random walkable cell within 7 cells"; rAthena's `MOB_LAZYMOVEPERC` cadence + 1–3 cell hops can land alongside the broader mob AI in MS3.

## Pending

### Items, in order

1. **`MobSpawnEntry` record:**
   ```csharp
   public record MobSpawnEntry(
       int MobClassId,
       uint MapId,
       short X, short Y, short Xs, short Ys, // area; 0 = anywhere
       int MinAmount,
       int RespawnDelayMs,
       int RespawnDelayJitterMs,
       string? EventOnDeath = null);
   ```

2. **`IMobSpawnRegistry`** — collected during NPC parsing (every `monster` line becomes a spawn entry). Initial pass at map load.

3. **`MobEntity`** with the runtime fields not in mob_db:
   - `EntityId Id`, type=MOB
   - `MapId, X, Y, Dir` (from `Entity` base)
   - `MobDbEntry DbEntry` (static catalog reference)
   - `uint Hp, Sp` (current — full at spawn)
   - `WalkState? Walk` — idle wander state
   - `DateTime? NextWanderUtc` — when to pick a new wander target
   - `MobSpawnEntry Origin` — backref for respawn scheduling

4. **Initial spawn algorithm.**
   - For each entry, instantiate `entry.MinAmount` mobs.
   - For each: pick a random walkable cell within `(entry.X-Xs, entry.Y-Ys) to (entry.X+Xs, entry.Y+Ys)`. If `Xs==0 && Ys==0`, pick any walkable cell on the map.
   - Add to `IEntityRegistry`, broadcast STANDENTRY to viewers.

5. **`MobSpawnService.Tick`** — every map tick:
   - For each idle mob, if `NextWanderUtc <= now`, pick a new wander target (random walkable cell within 7 cells), trigger `MovementService.TryStartWalk`. Set `NextWanderUtc = now + random(5s, 30s)`.
   - For each pending respawn, if `now >= respawnAt`, instantiate a fresh `MobEntity` at the origin entry's location.

6. **Death + respawn (admin path).** Add a GM command handler `@killmob` (or similar) that:
   - Look up `MobEntity` by entity id.
   - Set `Hp = 0`, broadcast `ZC_NOTIFY_VANISH` with `reason=DEAD`.
   - Remove from registry.
   - Schedule respawn via `MobSpawnService.ScheduleRespawn(entry, now + entry.RespawnDelayMs + jitter)`.

   Real combat-driven death is MS3.

7. **Mob name + chat bubble.** `ZC_NOTIFY_STANDENTRY` for mobs includes the name from `mob_db.Name` (or `JapaneseName` for some servers). No localization in MS2.

### File layout

```
Map.Server/Spawn/
├── MobSpawnEntry.cs
├── MobEntity.cs                 — full impl (was stub in entities.md)
├── IMobSpawnRegistry.cs
├── MobSpawnRegistry.cs          — collected from NPC parser
├── IMobSpawnService.cs
└── MobSpawnService.cs           — Tick, ScheduleRespawn, Death
```

### Tests

1. `MobSpawnEntryTests`: parse a `monster` line, build the right entry.
2. `MobSpawnServiceTests`:
   - Initial spawn: 3 entries × 5 amount = 15 mobs on the map.
   - Wander: after `NextWanderUtc` passes, mob walks to a new cell.
   - Death + respawn: kill a mob → entity removed → after `RespawnDelayMs`, new entity spawned at origin area.
   - Respawn jitter: 100 deaths → respawn times spread within the jitter window.
3. Smoke: load `prontera`'s spawn config (likely just town, no mobs); load `prt_fild01`'s mobs (Lunatic/Fabre/Pupa). Assert ≥1 of each present after initial spawn.

### Acceptance

- A new map starts with the configured mobs at their declared spots.
- Mobs idle-wander within their area.
- Killing a mob (GM command) removes it and respawns one within the delay.
- Players in view see `ZC_NOTIFY_STANDENTRY` on spawn, `ZC_NOTIFY_VANISH` on death, `ZC_NOTIFY_MOVE` on wander.

### Open decisions

- **Spawn at exact spot vs random within area.** rAthena: `(x=0, y=0)` means "anywhere walkable on the map"; otherwise random within the box. We mirror this.
- **Mob personal id (game_id) vs class id.** Class id is shared (all Porings have class 1002). Game id is per-instance (this Poring is entity `2_000_125`). Players see class id in name lookups but the wire-level entity id is unique. Mirror rAthena.
- **Respawn jitter default.** rAthena default `delay1=5s, delay2=2s` (jitter). We use the same.

## History

- **2026-05-16** — Plan written. No implementation yet.
- **2026-05-16** — Registry + service shipped. Initial spawn / wander / kill-and-respawn cover the MS2 acceptance criteria. NPC-driven entry population and GM kill packet remain follow-ups.
