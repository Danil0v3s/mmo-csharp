# GP-INSTANCE — Instances work end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** XL · **Player-visible:** yes
> **Depends on:** none (but contains the dynamic-map subsystem build) · **Unlocks:** SCR-DOMAIN (instance builtins)

## The deliverable

> A party can **create an instance, enter a private cloned copy of the dungeon populated with
> its NPCs + mob spawns, play it (mobs/NPCs isolated from other parties' copies), and have it
> auto-destroy on idle/keep timeout** — live client, with party members warped out on destroy.

## Player story

The instance *lifecycle* is real (keep/idle timers, auto-destroy, occupant eviction, owner
resolution, party/guild scoping — archive FEATURE-14). But **instances aren't enterable**:
`IMapWorldRegistry` is immutable, so there is no physical scoped map to warp onto, no NPC/mob
population, and no client UI. This ticket builds the **dynamic-map subsystem** (the hard
prerequisite) so instances actually exist, plus the packets.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Catalog | ✅ | `instance_db` seeded (enter map, additional maps, time/idle limits) |
| Lifecycle | ✅ verify | `Map.Server/Instance/InstanceService.cs` — create/enter-gate/timers/Tick/destroy/owner (archive FEATURE-14) |
| Dynamic maps | ❌ | `IMapWorldRegistry` is **immutable** — no clone/register/free (archive INFRA-12) |
| Map population | ❌ | NPC/mob spawn into the scoped map (blocked on dynamic maps) |
| CZ handlers | ❌ | instance create/enter/destroy/info UI |
| ZC emits | ❌ | instance create/info(remaining time)/state, map-warp |

## rAthena reference

- `rathena/src/map/instance.cpp` — `instance_create`, `instance_addmap` → `map_addinstancemap`
  (clone base `map_data` into the instance namespace), `instance_addnpc` →
  `npc_duplicate4instance` (clone NPCs onto the cloned map), `instance_destroy` →
  `map_delinstancemap` (free), the keep/idle timers, party/guild scoping.
- `rathena/src/map/map.cpp` — `map_addinstancemap`/`map_delinstancemap` (cell-grid + block-list
  clone/free per instance).
- `rathena/src/map/clif.cpp` — `clif_instance_create`/`clif_instance_status`/`clif_instance_changewait`.

## Dependencies — and how to satisfy

- **Dynamic-map subsystem** — the hard prerequisite, build it HERE (absorbs archive INFRA-12):
  make `IMapWorldRegistry` (or an overlay) accept runtime `RegisterInstanceMap(scopedName, clone)`
  + `RemoveInstanceMap`; deep-copy the base map's cell grid + a fresh per-instance spatial index;
  keep base maps immutable. This is the bulk of the ticket.
- Packet-bridge pattern — foundation.

## Scope — every layer

- [ ] **Dynamic maps**: mutable instance-map registration + `MapData` clone (cells/size/flags) +
      isolated per-instance entity/cell index; `Destroy` frees them.
- [ ] **Map population**: on create/add-map, clone the source map's NPCs + mob spawns into the
      scoped namespace (`instance_addnpc`-style); `Enter` warps onto the real cloned map.
- [ ] **Service**: verify the lifecycle at HEAD; `MapId`/`Enter` resolve the real cloned map id.
- [ ] **CZ handlers**: instance create (from NPC), enter, destroy, info request.
- [ ] **ZC emits**: instance create/info(remaining time)/state, map-warp on enter.

## Done criteria

- A party creates an instance → members enter a private cloned dungeon populated with its NPCs
  + mob spawns; killing a mob in one party's copy doesn't affect another party's copy.
- The instance auto-destroys on idle-empty or keep-timeout → occupants warp out to savepoint;
  the cloned maps are freed (no leak).
- Two concurrent instances of the same template are isolated.

## Test plan

- Dynamic-map: clone a base map → resolvable via the registry → freed on destroy → two clones
  isolated.
- Lifecycle: extend archived InstanceServiceTests (timers/destroy already covered).
- Handler tests: create/enter/destroy → service.
- Live: party create → enter populated map → idle auto-destroy.

## Notes / gotchas

- Scoped map name is `"{instanceId}@{baseName}"` (archive FEATURE-14).
- Cell-grid + AOI isolation per instance is the expensive part — each clone needs its own
  spatial index, not a shared one keyed by base map id.
- Player eviction on destroy already works (archive FEATURE-14) — wire the map-free into it.
