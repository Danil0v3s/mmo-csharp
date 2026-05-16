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

- **`EntityId` strong-typed wrapper** ([Map.Server/Entities/EntityId.cs](../../../Map.Server/Entities/EntityId.cs)) over `int` to avoid mixing with character/account/class ids. Documents rAthena's allocation ranges.
- **`EntityType` flags enum** matching rAthena `bl_type` (PC=0x001, MOB=0x002, PET, HOM, MER, ELEM, NPC, ITEM, SKILL, CHAT). Used as filter mask in spatial queries.
- **`Entity` abstract base** with `Id`, `Type`, `MapId`, `X`, `Y`, `Dir`. Subclasses: `PlayerEntity` (full MS1 impl: AccountId, CharacterId, Name, SessionId), `NpcEntity` (MS2 stub), `MobEntity` (MS2 stub).
- **`EntityIdAllocator`** with non-overlapping ranges per type (mob 400M+, npc 800M+, skill 1.5B+, item 2B+). Thread-safe via `Interlocked`.
- **`MapSpatialIndex`** — per-map bucketed cell grid (row-major HashSet array). Insert/Remove/Move/ForEachInRange/ForEachInArea, single coarse lock. Snapshot semantics on iterate.
- **`IEntityRegistry` + `EntityRegistry`** — singleton authoritative registry of all entities, lazy per-map spatial index creation, type-mask filtered queries.
- **Wired into Map.Server DI** ([Map.Server/Program.cs](../../../Map.Server/Program.cs)): `EntityIdAllocator` + `IEntityRegistry` as singletons.
- **Legacy `IPlayerMapService` migrated** to a facade over `IEntityRegistry`. The old struct-style `PlayerEntity` in `MapServerImpl.cs` is gone; the new `Map.Server.Entities.PlayerEntity` is what callers receive. Placeholder `EnterMapHandler` (pre-MS1 cruft listening on the wrong packet) deleted — [session.md](session.md) will add the real connect handler.
- **20 new tests** in `Map.Server.Tests/Entities/` covering `MapSpatialIndex` (insert/remove/move/range/area/bounds), `EntityIdAllocator` (range correctness, sequential uniqueness), and `EntityRegistry` (CRUD, mask filtering, move with index sync, unknown-map empty result).

Full suite: 148 char + 16 login + 40 map = **204 green** (up from 185).

## Pending — for future expansion

1. **Map index ↔ numeric map_id** — currently `MapSpatialIndex` is keyed by `uint mapId` derived from `name.GetHashCode()`. This works because the registry's hash matches whatever the caller passes. Once [world.md](world.md) lands a proper `MapIndex` (name ↔ small-int id from `map_index.txt`), switch to that for clarity and to align with the wire protocol's map_id field.
2. **`free_block_lock` parity** — `MapSpatialIndex.ForEachInRange` already returns a snapshot list, so the rAthena "delete during iteration" hazard is solved by construction. If a future hot-path query needs zero-allocation iteration, revisit with an explicit lock pattern.
3. **Movement integration** ([movement.md](movement.md)) — `EntityRegistry.Move(EntityId, x, y)` exists; the walk-loop tick will call it on each step. No changes needed here; [movement.md](movement.md) consumes the existing API.

### File layout (delivered)

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

## History

- **2026-05-16** — **MS1.entities shipped.** Built `EntityId`, `EntityType`, `Entity` + `PlayerEntity` + stub `NpcEntity`/`MobEntity`, `EntityIdAllocator`, `MapSpatialIndex` (bucketed cell grid), `IEntityRegistry` + `EntityRegistry`. Migrated `IPlayerMapService` to a facade over `EntityRegistry`. Deleted the placeholder `EnterMapHandler` and the legacy `ConcurrentDictionary<long, PlayerEntity>` / `ConcurrentDictionary<Guid, long>` DI registrations — they were pre-MS1 cruft listening on the wrong packet. The map server's `MapGrpcService.EnterMap` / `LeaveMap` / `GetMapInfo` / `ReceiveWhisper` / `ForceDisconnectAccount` and `MapServerImpl.SaveAllOnlinePlayersAsync` flows all updated. 20 new tests cover spatial index, id allocator, and registry CRUD/filter/move. Full suite 204 green.
- **2026-05-16** — Plan written. No implementation yet.
