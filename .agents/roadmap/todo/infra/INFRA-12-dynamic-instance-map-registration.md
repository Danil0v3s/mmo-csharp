# INFRA-12 — Dynamic instance-map registration + cloning

> **Epic:** Infra-World · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** FEATURE-14 (lifecycle state machine) · **Blocks:** instance content (populated instances)

## Problem

`IMapWorldRegistry` is **immutable** — "Built once at map server startup from the
configured mapcache.dat + map list; immutable thereafter." There is no way to add a
map at runtime. Instances therefore have **no physical scoped map**: the
`"{instanceId}@{baseName}"` names FEATURE-14 generates resolve to nothing, so
`instance_enter`'s `Setpos` to a scoped map fails, the template's NPCs/mob spawns
can't be world-spawned into the instance (so the map would be empty even if it
existed), and players can't actually stand on instance ground. FEATURE-14 landed the
full lifecycle state machine (keep/idle timers, owner, scoping, destroy/eviction) on
top of this gap, but **a player still cannot enter a real, populated instance** until
maps can be cloned at runtime.

## Current state (C#)

- `Map.Server/World/IMapWorldRegistry.cs` — `Get`/`All`/`Contains`/`TotalCells` only; **no Add/Register/Remove**. Doc explicitly says immutable.
- `Map.Server/World/MapWorldRegistry.cs` — `_byName` dictionary built once in the ctor; `Load(...)` reads mapcache at boot.
- `Map.Server/Instance/InstanceService.cs` (FEATURE-14):
  - `Create`/`AddMap` build scoped names (`GenerateMapName`) but register **no** map.
  - `Enter` warps via `_setpos.Setpos(pc, "{id}@{base}", x, y)` — fails because the scoped map isn't registered.
  - `AddNpc` tracks + registers the NPC entity for despawn bookkeeping, but it is **not client-visible** (no scoped map / cell grid to place it on).
- `Map.Server/Movement/PcSetposService.cs` — resolves the target map through `IMapWorldRegistry`; an unregistered scoped name can't be a warp target.

## rAthena reference (source of truth)

- `rathena/src/map/instance.cpp`:
  - `instance_addmap(instance_id)` → `map_addinstancemap(name, instance_id)` — **clones** the base `map_data` (cells, size, flags) into a new entry in the live `map[]` array under the instance id, returns the new map index `m`.
  - `instance_addnpc(idata)` → `map_foreachinallarea(instance_addnpc_sub, src_m, …, BL_NPC, m)` → `npc_duplicate4instance(nd, m)` — duplicates every NPC from the source map onto the cloned instance map, then runs `OnInstanceInit`.
  - `instance_destroy` → `map_delinstancemap(m)` — frees the cloned map + its block lists.
- `rathena/src/map/map.cpp`: `map_addinstancemap` / `map_delinstancemap` — the clone/free of `map_data` (cell array copy, fresh block_list grid, instance namespace).

## Scope — every sub-system that must be touched

- [ ] Make the map registry mutable for instance namespace: add `RegisterInstanceMap(string scopedName, MapData clone)` + `RemoveInstanceMap(string scopedName)` to `IMapWorldRegistry` (or a dedicated `IInstanceMapRegistry` overlay the lookup falls through to), preserving the "base maps immutable" guarantee.
- [ ] `MapData` clone: deep-copy the base map's cell grid + size + flags into the scoped instance map; allocate a fresh per-instance cell/entity spatial index so AOI on the instance map is isolated.
- [ ] `InstanceService.AddMap`/`Create` → clone each template map into the scoped namespace via the new registry API.
- [ ] `InstanceService.AddNpc` + a new `PopulateInstanceMaps` step → world-spawn the source map's NPCs (and scoped mob spawns) onto the cloned map so it is non-empty (mirror `npc_duplicate4instance`).
- [ ] `Enter` → now warps onto the real cloned map (Setpos resolves it).
- [ ] `Destroy` → free the cloned maps via `RemoveInstanceMap` (FEATURE-14 already evicts occupants + despawns the tracked NPCs; wire the map free here).
- [ ] `MapId(baseMapId, instanceId)` → return the real cloned map id, not the hash placeholder.

## Done criteria

- Entering an instance lands the player on a real cloned map populated with the template's NPCs + mob spawns (no longer empty / no longer a failed warp).
- `Destroy` frees the cloned map(s) — no leaked `MapData` / spatial index.
- Base (non-instance) maps remain immutable; two concurrent instances of the same template have isolated maps (a mob death in one doesn't affect the other).

## Test plan

- `Map.Server.Tests`: clone a base map → scoped map resolvable via the registry; spawn a template NPC onto it; enter places the PC on the cloned map; destroy frees it (registry no longer resolves the scoped name); two instances of the same template are isolated.

## Notes / gotchas

- This is the missing **physical** half of instances; FEATURE-14 is the **logical** half (lifecycle/timers/scoping/owner/eviction) and already works on top of this seam.
- Cell-grid + AOI isolation per instance is the expensive part — each clone needs its own entity spatial index, not a shared one keyed by base map id.
- Keep the base-map-immutability invariant: only the instance namespace is mutable.
