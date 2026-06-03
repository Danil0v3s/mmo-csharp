# SCRIPT-06 — Instance builtins (instance_create / destroy / enter / warpall / … / getinstancevar)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-14 (instance subsystem), SCRIPT-05 (party/guild member checks), SCRIPT-07 (per-instance var scope) · **Blocks:** SCRIPT-10 (instanced dungeons)

## Problem

Instanced content (memorial dungeons, Endless Tower, party/guild instances) is entirely
non-functional from script. `ctx.instance.*` is all stubs: `create` returns `0`, `enter`
returns `0`, `warpAll`/`announce`/`destroy` no-op, the `check*` gating returns `false`,
`getVar`/`setVar` (the per-instance variable scope) returns null. An instance NPC can run
its whole onClick flow and never actually spin up an instance map, never warp the party in,
and never store instance-local progress.

## Current state (C#)

- `Map.Server/Scripting/Dialog/SubsystemContexts.cs:50-85` — `InstanceContext`: `create`,
  `destroy`, `enter`, `npcName`, `mapName`, `id`, `warpAll`, `announce`, `checkParty`,
  `checkGuild`, `checkClan`, `info`, `liveInfo`, `list`, `getVar`, `setVar` — all
  `ScriptStub`. That's 16 methods covering the rAthena instance builtin set.
- `Map.Server/Instance/` — instance service dir exists (FEATURE-14 target). Verify whether it
  models instance lifecycle (allocate id, clone source map block, assign owner party/guild/char,
  TTL, idle timeout) or is itself a shell.
- Per-instance vars: rAthena's `'var` scope is keyed by instance id. SCRIPT-07 owns the var
  consolidation; this ticket adds the *instance* scope (`getinstancevar`/`setinstancevar`).
- Map cloning: instance maps are runtime copies of a template map (`1@xxx` naming). Needs the
  map block-list / cell data clone + NPC duplication into the instance.

## rAthena reference (source of truth)

`script.cpp` + `instance.cpp`.

- `script.cpp:21602 BUILDIN(instance_create)` → `instance_create(owner_id, name, mode)`:
  allocates an instance id, reads the `instance_db` entry (maps to clone, idle/timeout, enter
  point), clones the listed maps, returns the instance id (negative codes on failure:
  `-1` no db, `-2` no maps free, `-3` owner already has one, `-4` invalid owner).
- `script.cpp:21683 BUILDIN(instance_enter)` → `instance_enter(sd, instance_id, name, x, y)`:
  warps the player to the instance's enter coords (or the db default). Returns 0 ok / 1 no
  instance / 3 nonexistent.
- `script.cpp:21839 BUILDIN(instance_warpall)` → warp everyone in the instance's source-side
  party/guild to (map,x,y) inside the instance.
- `script.cpp:21657 BUILDIN(instance_destroy)` → free maps, kick occupants to fallback, clear vars.
- `script.cpp:21709/21739 instance_npcname / instance_mapname` → resolve a template npc/map name
  to its instanced (`1@xxx`) name for the live instance.
- `script.cpp:21764 instance_id` → current instance id from the running script's context.
- `script.cpp:21882 instance_announce` → broadcast to all maps in the instance.
- `script.cpp:21918/22004/22066 instance_check_party / _guild / _clan` → gate: party/guild/clan
  has ≥ `amount` members within `[minLv,maxLv]`. Returns bool.
- `script.cpp:22125/22199/22251 instance_info / instance_live_info / instance_list` — db info,
  live state (occupants, remaining time), and the list of live instances of a template.
- `getinstancevar`/`setinstancevar` (the `'var` scope) — per-instance numeric/string store,
  freed on destroy.

## Scope — every sub-system that must be touched

- [ ] **Instance lifecycle** (FEATURE-14, implement if shell): `IInstanceService` with
      `Create(ownerId, templateName, mode)` (alloc id, load `instance_db`, clone maps + NPCs,
      set TTL/idle), `Destroy(id)` (free maps, evict players, clear vars), occupant tracking.
- [ ] **`InstanceContext`** — delegate every method: `create`→service (return id/neg code),
      `enter`→warp to instance enter point, `warpAll`→warp all occupants, `destroy`,
      `npcName`/`mapName`→name resolver, `id`→running-script instance id, `announce`→instance
      broadcast, `checkParty/Guild/Clan`→member-count gate (reuse SCRIPT-05 roster fetch),
      `info`/`liveInfo`/`list`→db + live queries, `getVar`/`setVar`→instance var scope.
- [ ] **Per-instance var scope** — add `VarScope.Instance` (or an instance-keyed store) to the
      consolidated var service (SCRIPT-07), keyed by instance id, freed on destroy.
- [ ] **Running-script instance context** — a script running inside an instanced NPC must know
      "which instance am I in" so `instance_id`/`instance_npcname` resolve. Thread the instance
      id onto `DialogContext` / `NpcInfo` when the NPC belongs to an instance.
- [ ] **Map cloning** — duplicate the template map's cell grid + block list + NPC set into the
      runtime `1@xxx` map (or whatever the project's map-instance naming is). Register the cloned
      NPCs with the script engine (their hooks must fire in the instance).

## Done criteria

- `const iid = await ctx.instance.create("1@nyd", partyOwnerId)` returns a positive id; the
  template maps are cloned and addressable as their instanced names.
- `instance.enter("1@nyd")` warps the player into the cloned map at the db enter point.
- `instance.warpAll(mapName,x,y)` moves all occupants; `instance.destroy()` frees maps and
  evicts players to the fallback point.
- `instance.checkParty(pid, 3, 1, 99)` returns true only when ≥3 members in level range.
- `instance.setVar("phase", 2)` then `getVar("phase")` returns `2`, isolated per instance id,
  and is gone after destroy.
- `instance.npcName("#warp_out")` / `instance.mapName("1@nyd")` resolve to the live instance names.
- **No `ScriptStub.Call` left in `InstanceContext`.**

## Test plan

- `Map.Server.Tests/Scripting/InstanceBuiltinsTests.cs`: fake `IInstanceService`; assert
  `create` returns the service id; `enter` issues a warp to the resolved enter point; `checkParty`
  gating matches a faked roster; `setVar`/`getVar` round-trip and are scoped per instance id and
  cleared on `destroy`.
- Map-clone test: create instance of a 2-NPC template → both NPCs present in the clone with
  working hooks (fire OnInit in the instance).

## Notes / gotchas

- HARD-GATED on FEATURE-14. If the instance subsystem is a shell, that work is the bulk of this
  ticket — don't ship `InstanceContext` delegating to stubs.
- Negative return codes from `instance_create` are load-bearing — instance scripts branch on
  exact values (`-3` = "you already have an instance"). Match them.
- The "which instance am I in" plumbing is the subtle part: a cloned NPC's script must see its
  own instance id, not 0. Without it `instance_id`/`*name` resolution silently targets the
  template.
- Free instance vars on destroy or they leak across reuse of the same instance id.
