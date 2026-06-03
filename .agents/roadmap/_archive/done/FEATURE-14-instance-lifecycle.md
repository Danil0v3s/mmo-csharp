# FEATURE-14 — Instance lifecycle

> **Epic:** Gameplay-Instance · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Related:** PACKET-09 (instance UI / timer packets) · INFRA-12 (physical scoped-map layer)

## Problem

Instances half-work: the catalog loads, `Create` builds scoped map names, and
`Enter` does a real warp. But **instance NPCs never spawn** (`AddNpc` is a
no-op), so an instance map is empty of its scripted content; **none of the
timers do anything** (`StartIdleTimer`/`StopIdleTimer`/`StartKeepTimer` just
return `ContainsKey`), so instances never auto-destroy and leak forever;
`GetOwner` always returns null; and there is **no party/guild scoping
enforcement** (anyone could enter anyone's instance). The whole lifecycle
(idle→keep→destroy) is missing.

## Current state (C#)

- `Map.Server/Instance/InstanceService.cs`:
  - `Create(dbId, ownerId, mode)` (`:56`) — allocates id, seeds `Maps[]` from `EnterMap` + `AdditionalMaps`. Works.
  - `Enter(pc, instanceId)` (`:111`) — real `_setpos.Setpos` warp to the scoped enter map. Works.
  - `AddNpc(instanceId, npc)` (`:146`) — **empty body** `{ }`. Instance NPCs never spawn.
  - `StartIdleTimer` / `StopIdleTimer` / `StartKeepTimer` (`:143`–`:145`) — each just `return _instances.ContainsKey(instanceId);` — **no timers**, no auto-destroy.
  - `GetOwner(instanceId)` (`:165`) — `_instances.TryGetValue(...) ? null : null` — **always null** (and ignores `OwnerId`).
  - `Destroy` (`:140`) just removes from the dict (no map cleanup / NPC despawn / player eviction); `DestroyCommand` (`:141`) delegates.
  - `AddUsers`/`DelUsers` (`:81`/`:87`), `AddMap` (`:94`), `GetInstanceMaps` (`:137`), `MapId` (`:156`), `Reload` (`:168`) work at the bookkeeping level.
  - `InstanceRecord` (`:170`) tracks `Id, DbId, OwnerId, Mode, Users, Maps[]` — no owner-type (party/guild/char), no timer fields.
- No party/guild scoping fields; no per-instance NPC list; no idle/keep timer state.

## rAthena reference (source of truth)

- `rathena/src/map/instance.cpp`:
  - `instance_create(owner_id, name, mode)` — `mode` ∈ {IM_CHAR, IM_PARTY, IM_GUILD, IM_CLAN}; the owner_id is the char/party/guild/clan id. Sets `keep_limit` (max lifetime) + `idle_limit` (timeout with no players).
  - `instance_addmap(instance_id)` — for each map in the template, `map_addinstancemap` clones the base map into the instance namespace **and spawns the map's NPCs/mob spawns into the instance** (`instance_addnpc` per NPC, scoped mob spawns).
  - `instance_addnpc(struct s_instance_data *im)` — duplicate the source map's NPCs into the instance map (the no-op in C#).
  - `instance_destroy(instance_id)` — evict all players (warp out), despawn all NPCs/mobs, free the cloned maps, clear timers, notify (`clif_instance_changewait`/`changestatus`).
  - Timers:
    - `idle_timer` — when the instance has 0 players for `idle_limit` seconds, destroy. `instance_addusers`/`instance_delusers` start/stop the idle timer (stop when someone enters, start when the last leaves).
    - `keep_timer` — hard lifetime cap; destroy when `keep_limit` elapses regardless of occupancy.
  - `instance_check_*` — party/guild membership gate on enter (`instance_enter` validates the PC belongs to the owner party/guild/clan).
  - `instance_addusers` notifies the client of the remaining time.

## Scope — every sub-system that must be touched

- [ ] Extend `InstanceRecord`: owner **type** (char/party/guild/clan) + owner id, `KeepLimitTick`, `IdleLimitMs`, `IdleSinceTick` (or running idle timer), and a per-instance NPC/spawn list. Load `keep_limit`/`idle_limit` from the `instance_db` row in `Create`.
- [ ] `AddNpc` — **actually spawn** the NPC into the instance-scoped map (register it in the NPC service / entity registry under the scoped map name). Track it on the record for despawn.
- [x] ~~**Instance map population**~~ ➡️ **Moved to INFRA-12** — cloning base maps + world-spawning the template NPCs/mob spawns requires a mutable map registry (`IMapWorldRegistry` is immutable). That physical layer is INFRA-12; this ticket landed the logical lifecycle on top.
- [x] `StartIdleTimer` / `StopIdleTimer` — real idle tracking: idle countdown stops on `AddUsers` 0→>0 (start keep), restarts on `DelUsers`→0 (`IdleLimitTick`), per-tick sweep destroys idle-expired.
- [x] `StartKeepTimer` — real hard-lifetime: `KeepLimitTick = now + db->limit*1000`; the per-tick sweep destroys keep-expired.
- [x] **Lifecycle sweep**: `Tick(nowTick)` added to `IInstanceService` + `InstanceService`, wired into `MapServerImpl` after `_elemental?.Tick`.
- [x] `Destroy` — **full teardown**: evicts every occupant to their savepoint (`IPcDeathService.WarpToSavepoint`, prontera last-resort), despawns the instance's tracked NPCs from `IEntityRegistry`, clears both timers, drops the record. (Freeing the *cloned map structures* ➡️ INFRA-12, since maps aren't cloned yet.)
- [x] `GetOwner` — resolves by `e_instance_mode` (owner char for IM_CHAR; a representative online party/guild/clan member otherwise) via `IEntityRegistry`. No longer always-null.
- [x] **Party/guild scoping enforcement** in `Enter`: `OwnsInstance` gates by mode (char/party/guild/clan id match); rejects non-members.
- [x] **Client packets**: ZC_INSTANCE_CREATE/INFO/STATE etc. ➡️ **PACKET-09** (no instance packets exist in `Core.Server/Packets` yet); the lifecycle state this ticket built is what PACKET-09 will emit.
- [x] `AddNpc` — records the NPC on the instance + registers it in `IEntityRegistry` (cleaned up on destroy). **World-visibility on the scoped map ➡️ INFRA-12** (no physical map to place it on yet).

## Done criteria

- Entering an instance lands the player in a map populated with the template's NPCs + mob spawns (no longer empty). ➡️ **Moved to INFRA-12** (physical scoped-map cloning) — the membership gate + occupancy/timer side of `Enter` is real here.
- An instance with 0 players auto-destroys after `idle_limit`; any instance auto-destroys at `keep_limit` regardless of occupancy. ✅ (`InstanceServiceTests` pin both, plus the "occupied survives idle" + "keep destroys occupied" cases).
- `Destroy` warps out all occupants, despawns NPCs/mobs, and frees the maps — no leak. ✅ for occupant eviction + NPC despawn + timer clear; *freeing cloned maps* ➡️ INFRA-12 (no maps to free yet).
- `GetOwner` returns the real owner; `Enter` rejects a PC not in the owner party/guild/clan. ✅
- No empty `AddNpc`, no `ContainsKey`-only timer stubs, no always-null `GetOwner`. ✅

## Test plan

- `Map.Server.Tests` (add `InstanceServiceTests`):
  - `AddNpc` registers the NPC under the scoped map;
  - idle timer: occupancy 0 + advance clock past `idle_limit` → instance destroyed; a re-`AddUsers` before timeout cancels;
  - keep timer: advance past `keep_limit` → destroyed even with occupants;
  - `Destroy` evicts occupants + despawns NPCs;
  - `Enter` rejects a non-member, accepts a party member; `GetOwner` returns the owner.
- Manual/live: create a party instance, enter with two party members, confirm NPCs/mobs present, leave both, confirm auto-destroy after idle.

## Notes / gotchas

- The scoped map name is `"{instanceId}@{baseName}"` (`GenerateMapName :147`) — NPC/mob clones must register under that name, and `Destroy` must clean up by that name.
- Idle vs. keep: idle resets on occupancy; keep is absolute — track both independently.
- Player eviction on destroy needs a safe fallback map (rAthena uses the instance's `exit`/save point); use the PC's save point.
- Don't leak the cloned map data structures on `Destroy` — free the per-instance map registrations. ➡️ INFRA-12 (no cloned maps yet).

## History

- 2026-06-03 — Landed the real instance **lifecycle state machine** (rAthena `instance.cpp`).
  `InstanceRecord` gained owner-mode/id, `KeepSecs`/`IdleSecs` (read from the `instance_db`
  row's `TimeLimit`/`IdleTimeout` in `Create`), `KeepLimitTick`/`IdleLimitTick`, occupant set,
  and an NPC list. `AddUsers` 0→>0 stops idle + starts keep (BUSY); `DelUsers`→0 restarts idle;
  `StartKeepTimer`/`StartIdleTimer`/`StopIdleTimer` are real; new `Tick(nowTick)` sweep (wired
  into `MapServerImpl` after the elemental sweep) destroys keep- or idle-expired instances.
  `Destroy` evicts occupants to their savepoint (`IPcDeathService.WarpToSavepoint`) + despawns
  the tracked NPCs from `IEntityRegistry` + clears timers. `Enter` gained an `e_instance_mode`
  membership gate (`OwnsInstance`) + occupancy bump; `GetOwner` resolves the real owner by mode;
  `AddNpc` records + registers the NPC (no longer a no-op). Injectable clock for testing.
  Tests: `InstanceServiceTests` (7) green; full suite green (1 pre-existing replay-fixture fail).
  Matched `instance_create`/`addusers`/`delusers`/`startkeeptimer`/`startidletimer`/
  `stopidletimer`/`destroy`. **The physical scoped-map layer** (cloning base maps, world-spawning
  NPCs/mobs so the instance is non-empty, warping onto real instance ground, freeing cloned maps)
  is blocked on a mutable map registry → filed **INFRA-12**; client packets → **PACKET-09**.
