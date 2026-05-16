# MS1 · Entity model — block list, lifetime

**Phase:** MS1
**Depends on:** [world.md](world.md) (needs `MapData` for spatial lookup)
**Blocks:** session, movement, visibility, everything in MS2+

rAthena's `struct block_list` (`bl`) is the abstract base for every entity that exists in the world: players, NPCs, mobs, items on the floor, skill objects, pets, homunculi, mercenaries, elementals. Every gameplay loop is `map_foreachinrange(...)` over this list. We need a clean port of this model before anything else can happen.

## Source of truth

- [rathena/src/map/map.hpp](/Volumes/1TB/Projetos/rathena/src/map/map.hpp) — `struct block_list`, `enum bl_type`, the per-map `block_list_head` table
- [rathena/src/map/map.cpp](/Volumes/1TB/Projetos/rathena/src/map/map.cpp) — `map_addblock`, `map_delblock`, `map_foreachinrange`, `map_foreachinarea`, `map_freeblock`
- [rathena/src/map/unit.cpp](/Volumes/1TB/Projetos/rathena/src/map/unit.cpp) — `unit_data`, lifecycle (`unit_remove_map`, `unit_free`)

## Scope (MS1)

**In scope:**
- Abstract `Entity` base with type tag (PC, NPC, MOB, ITEM, SKILL, ELEM, MER, HOM, PET), unique id, current map, current (x, y).
- Per-map spatial index for `foreachinrange` / `foreachinarea` queries.
- Lifecycle: add to map, remove from map, free; safe-iteration semantics (rAthena's `map_freeblock_lock` / `unlock` pattern for the "delete-during-iteration" case).
- Entity id allocation (global monotonic int).
- Just the **player** subtype is fully populated for MS1. The other types are placeholders/scaffolding until MS2 adds NPCs and mobs.

**Out of scope:**
- Combat / unit_data fields (HP, status flags, attack speed) — MS3.
- Item-on-floor objects — MS3 items.
- Skill objects (`skill_unit_group`) — MS3 skills.

## Done

Map.Server has a placeholder `PlayerEntity` ([Map.Server/MapServerImpl.cs:122](../../../Map.Server/MapServerImpl.cs)) tracked via `IPlayerMapService`. That's a flat dictionary keyed by character id; no spatial index, no type hierarchy. Treat it as a temporary stub — it'll be replaced.

## Pending

### Items, in suggested order

1. **`EntityId` strong type.** Wrap an `int` to avoid mixing with character_id, account_id, mob class id, etc. Mirrors rAthena's `bl->id` (a globally unique int allocated per spawned entity).

2. **`EntityType` enum** with the rAthena values (`BL_PC=0x001`, `BL_MOB=0x002`, `BL_PET=0x004`, `BL_HOM=0x008`, `BL_MER=0x010`, `BL_ELEM=0x020`, `BL_NPC=0x040`, `BL_ITEM=0x080`, `BL_SKILL=0x100`, `BL_CHAT=0x200`). Bit flags so `map_foreachinrange(..., BL_PC | BL_MOB, ...)` patterns work.

3. **`Entity` abstract** with: `EntityId Id`, `EntityType Type`, `uint MapId`, `short X`, `short Y`, `byte Dir`. No virtual methods yet — the gameplay tick reads these fields directly.

4. **Subclasses for MS1:**
   - `PlayerEntity : Entity` — `int AccountId`, `int CharacterId`, `string Name`, `Guid SessionId`, plus walk state (target cell, walk timer, path queue — see [movement.md](movement.md)). Replaces the existing flat-dictionary `PlayerEntity` in MapServerImpl.
   - Stub classes for `NpcEntity`, `MobEntity` — populated in MS2.

5. **Per-map spatial index.** Two viable approaches:
   - **(A) Bucketed cell grid (rAthena's choice).** Each cell stores a linked list of entities in that cell. Movement updates the index by removing from old cell + inserting to new. `foreachinrange(map, x, y, range)` iterates the (2·range+1)² cells.
   - **(B) Per-map flat list, scanned linearly each query.** Simple, slow.

   Recommendation: **(A)** — matches rAthena, scales to 1000+ entities per map. Wrap as `MapSpatialIndex` and unit-test independently.

6. **`IEntityRegistry`** per-map service:
   - `Entity? Get(EntityId)`
   - `void Add(Entity, MapData)` (also calls `MapSpatialIndex.Insert`)
   - `void Remove(EntityId)`
   - `IEnumerable<Entity> ForEachInRange(uint mapId, short cx, short cy, short range, EntityType mask)`
   - `IEnumerable<Entity> ForEachInArea(uint mapId, short x0, short y0, short x1, short y1, EntityType mask)`

7. **EntityId allocation.** rAthena uses `MIN_FLOORITEM = 2000000` for items and a global counter elsewhere. Replicate the ranges so generated ids are recognizable in logs:
   - PCs: char_id (already unique).
   - Mobs/NPCs/Items: dedicated ranges to avoid collisions.

8. **Free-block-lock pattern.** rAthena defers deletion during iteration via `map_freeblock_lock()` / `map_freeblock_unlock()`. In C#, the equivalent is to maintain a "pending removals" set inside `ForEachInRange`'s caller scope and apply removals after the iteration. Or use a more idiomatic approach: copy the candidate list before iterating. Decide and document; the rAthena pattern exists because iteration-during-mutation is real.

9. **Replace `IPlayerMapService` with the new registry.** The current usage in [MapServerImpl.cs](../../../Map.Server/MapServerImpl.cs) and [MapGrpcService.cs](../../../Map.Server/MapGrpcService.cs) needs to switch over. The character-id → session-id map can be derived from the new `PlayerEntity` (which holds both).

### File layout

```
Map.Server/Entities/
├── EntityId.cs               — readonly record struct
├── EntityType.cs             — flags enum (BL_*)
├── Entity.cs                 — abstract base
├── PlayerEntity.cs           — full impl for MS1
├── NpcEntity.cs              — stub for MS2
├── MobEntity.cs              — stub for MS2
├── IEntityRegistry.cs        — interface
├── EntityRegistry.cs         — singleton implementation
├── MapSpatialIndex.cs        — per-map bucketed cell grid
└── EntityIdAllocator.cs      — central id allocator
```

### Tests (Map.Server.Tests)

1. `MapSpatialIndexTests`:
   - Insert one entity, `ForEachInRange` returns it; remove, returns empty.
   - Insert multiple, `ForEachInArea` returns the correct subset.
   - Move (remove+reinsert at new cell) — entity moves between bucket lists.
   - Edge cases: range = 0 (single cell), range covers more than the map.
2. `EntityIdAllocatorTests`:
   - Sequential ids never collide.
   - PCs use their own char_id; mobs use the dedicated range.
3. `EntityRegistryTests`:
   - Add → Get → Remove round-trip.
   - `ForEachInRange` mask filtering (PC-only vs PC|MOB vs ALL).
4. Concurrency note: registry is **not** required to be thread-safe — gameplay runs single-threaded on the tick. Document this.

### Acceptance

- Two players spawned on the same map, both visible to a `ForEachInRange(map, cx, cy, 14, BL_PC)` from each other's position.
- After one player moves, the spatial index reflects the new bucket (verifiable by another `ForEachInRange` call).
- `EntityRegistry.Remove` followed by `Get` returns null.

## History

- **2026-05-16** — Plan written. No implementation yet.
