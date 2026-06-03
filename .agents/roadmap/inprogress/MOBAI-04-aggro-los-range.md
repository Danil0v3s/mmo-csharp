# MOBAI-04 — Gate aggro on line-of-sight + reachable range

> **Epic:** Mob AI parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none

## Problem

The aggressive target scan in `MobAiService.Tick` finds the closest PC purely by
Chebyshev distance within view range — it does **not** check line-of-sight or
walkable path. A mob therefore **aggros through a wall**: a player standing behind
solid terrain, on the far side of a gat-blocked cell, or otherwise unreachable
will still be picked as a target, and the mob will walk into the wall trying to
reach them. rAthena's `mob_ai_sub_hard_activesearch` gates every candidate on
`status_check_skilluse` (visibility) and, under `ACTIVEPATHSEARCH`, a real
`path_search` walk-length check; the chase/attack later relies on
`battle_check_range`. None of that is present in the C# scan.

A second, parity-neutral issue: the scan iterates `_entities.All()` (every entity
on every map) and filters by map inline, rather than a cell-grid range query. This
is an O(N) full-registry walk per aggressive mob per think-tick. It produces
correct results (the map+distance filter is applied) but does not scale. That is
flagged as a follow-up, not the core of this ticket.

## Current state (C#)

- `Map.Server/Mob/MobAiService.cs:222-236` — the aggressive scan:
  ```csharp
  foreach (var other in _entities.All())          // <-- full registry, not a grid query
  {
      if (other is not PlayerEntity pc) continue;
      if (pc.MapId != mob.MapId) continue;
      if (pc.Hp <= 0) continue;
      var dist = Math.Max(Math.Abs(pc.X - mob.X), Math.Abs(pc.Y - mob.Y));
      if (dist > viewRange) continue;
      if (dist < closestDist) { closestDist = dist; closest = pc; }
  }
  ```
  **No LOS check, no path/reach check, no level/enemy filter.** The winner is
  handed straight to `_attack.StartAttack(mob, closest.Id, continuous: true)`
  (`:248`).
- `Map.Server/Mob/MobAiService.cs:283 _entities.ForEachInRange(mapId, x, y, range, EntityType.Pc)`
  — the cell-grid range query **is already used** by `HasAnyPcInView` (`:283`) and
  `SpotPcsInView` (`:330`). The aggressive scan does **not** use it (it predates the
  grid query or was never migrated).
- `Map.Server/Pathing/IPathService.cs:35 PathSearchLong(uint mapId, short x0, short y0, short x1, short y1)`
  — the Bresenham wall/LOS check (`PathService.cs:60`). Already consumed by several
  skill behaviors (`FrostyMisty.cs:43`, `FrostNova.cs:42`, `SightRasher.cs:51`,
  `ChargeAttack.cs:32`) for splash LOS. **Not injected into `MobAiService`** — the
  ctor (`MobAiService.cs:36-100`) takes no `IPathService`.
- `MobAiService` ctor — no `IPathService` param; DI registration at
  `Map.Server/Program.cs:416` constructs `MobAiService`, and `IPathService` is
  registered at `Program.cs:501`, so it is available to inject.
- Range/reach for the *engage*: `_attack.StartAttack` validates range internally,
  but the scan can still **select** an unreachable target, causing the mob to
  pick→walk→fail repeatedly. rAthena rejects unreachable candidates at scan time.

## rAthena reference (source of truth)

Canonical: `rathena/src/map/mob.cpp` (monolithic).

- **Active search** `mob_ai_sub_hard_activesearch` (`mob.cpp:1293-1343`), called via
  `map_foreachinallrange(..., view_range, DEFAULT_ENEMY_TYPE(md), ...)`
  (`mob.cpp:1873`):
  - skip self / `!status_check_skilluse(md, bl, 0, 0)` (visibility + usable)
    (`:1306`),
  - TARGETWEAK level skip (`:1309` — MOBAI-03),
  - `battle_check_target(md, bl, BCT_ENEMY) <= 0` skip (`:1312`),
  - gangster-paradise / homun-first skips (`:1315-1321`),
  - pick the closest passing target *and* require
    `battle_check_range(md, bl, db->range2)` (`:1326`),
  - **under `ACTIVEPATHSEARCH`** (`:1328-1336`): run `path_search(...CELL_CHKWALL)`;
    reject if no path, or if path length exceeds `range2` (standing mob) / `range3`
    (walking mob). This is the wall/reach gate.
- **`status_check_visible`** (the visibility half of `status_check_skilluse`):
  rejects targets the mob cannot see (hidden, cloaked, or out of sight). For the
  C# port the closest analogue is `IPathService.PathSearchLong` (Bresenham
  wall-blocked check) for the wall case, plus the existing hidden/cloak SC checks
  if available.
- **`path_search_long`** (`path.cpp`) — the long-range Bresenham LOS used for
  shootable/visible checks; this is exactly what `IPathService.PathSearchLong`
  ports (already used by skill splash LOS in this repo).
- **`battle_check_range`** (`battle.cpp`) — combines distance with a `path_search`
  shootable check; used to confirm a target is actually reachable/attackable within
  the weapon range before engaging.

## Scope — every sub-system that must be touched

- [ ] **Inject `IPathService`** into `MobAiService` (optional ctor param, defaulted
      null so existing tests keep compiling; register via DI — it is already a
      singleton at `Program.cs:501`).
- [ ] **Switch the aggressive scan to the cell-grid query**: replace the
      `_entities.All()` loop (`MobAiService.cs:224`) with
      `_entities.ForEachInRange(mob.MapId, mob.X, mob.Y, (short)viewRange, EntityType.Pc)`,
      matching `HasAnyPcInView`/`SpotPcsInView`. This removes the cross-map filter
      and the full-registry walk in one move (and is the perf follow-up's first
      half — doing it here is parity-neutral and cheap).
- [ ] **LOS gate**: for each candidate, before accepting it as `closest`, require
      `_paths.PathSearchLong(mob.MapId, mob.X, mob.Y, pc.X, pc.Y)` to return true.
      A wall-blocked PC is skipped. When `_paths` is null (tests), preserve current
      behavior (treat LOS as clear) so unit tests without pathing still pass — but
      the production wiring always supplies it.
- [ ] **Range/reach gate**: only accept a candidate the mob can actually reach.
      Mirror `battle_check_range` semantics: the candidate must be within view and
      LOS-clear; additionally, if a `path_search` walk-length check is available
      (`IPathService` long/short variants), reject candidates whose path length
      exceeds the mob's chase range (`range3` analogue). If only `PathSearchLong`
      exists, use LOS + distance as the reach proxy and document that the
      walk-length cap is approximated by LOS+distance.
- [ ] **Enemy filter**: keep the existing PC-only filter, but ensure the candidate
      passes a `battle_check_target(BCT_ENEMY)` analogue (no friendly/own-party
      aggro). If an `IBattleTargetService`/enemy-check exists, use it; otherwise the
      PC-vs-mob default is enemy and the filter is the existing alive+map check.
- [ ] **Apply the same LOS/reach gate to MOBAI-03's CHANGECHASE scan** if that
      ticket has landed, so chase-retarget also respects walls (coordinate; the
      `TryChangeChase` helper should take the same `IPathService`).
- [ ] **Perf follow-up note** (do NOT implement, just record): the grid query in
      `ForEachInRange` may itself iterate a coarse bucket; a true cell-grid scan
      (rAthena `map_foreachinrange` over the block list) is the scalable form. Flag
      as a parity-neutral optimization follow-up.
- [ ] No EF migration, no packets.

## Done criteria

- A mob does **not** aggro a PC standing behind a wall (LOS blocked) even when the
  PC is within view-range Chebyshev distance; it aggros the same PC once the wall
  no longer blocks the line.
- An aggressive mob only selects targets it can reach; it no longer
  walk-into-wall-loops toward an unreachable PC.
- The aggressive scan uses `ForEachInRange` (cell-grid query), not
  `_entities.All()`; results are identical for the in-LOS, in-range case (existing
  aggro behavior preserved for clear lines).
- `MobAiService` injects `IPathService`; with `IPathService` null (tests) the scan
  degrades to the prior distance-only behavior so no test regresses.
- No `// TODO`, no full-`All()` registry walk in the aggro scan, no log-only no-op.

## Test plan

- `Map.Server.Tests` `MobAggroLosTests` (new):
  - **wall block**: stub `IPathService.PathSearchLong` to return false for a PC
    cell → that PC is not aggroed though within view; return true → aggroed.
  - **closest in-LOS wins**: two PCs in view, the nearer behind a wall (LOS false),
    the farther clear → the farther (reachable) PC is selected.
  - **grid query**: assert the scan calls `_entities.ForEachInRange` (not `All()`)
    — verify via a spy registry that `All()` is not the source for the aggro pass.
  - **null path service**: no `IPathService` → falls back to distance-only (current
    behavior) and still aggros the closest PC.
- Regression: existing aggressive-scan tests (closest-PC, view-range cutoff, wander
  when none) still pass with LOS clear.
- Manual/live: place a wall between a mob and a char in a test map → the mob ignores
  the char until the char rounds the corner into LOS.

## Notes / gotchas

- `PathSearchLong` is the **shootable/LOS** Bresenham check (no diagonal-wall
  squeeze), already used for skill splash LOS in this repo — reuse it, do not write
  a new ray-cast.
- Keep the null-`IPathService` fallback so the large existing `MobAiService` test
  suite (which constructs the service without pathing) keeps compiling and passing;
  production DI always supplies it.
- The cell-grid scan in `ForEachInRange` already powers `HasAnyPcInView` — switching
  the aggro scan to it is consistent and removes the cross-map bug where `All()`
  could (in principle) consider entities on other maps before the inline filter.
- `battle_check_range` in rAthena combines distance **and** a shootable path check;
  for melee aggro the practical gate is LOS + within chase range. Do not over-gate
  (a mob should still aggro a PC it must walk a few cells to reach — only reject
  genuinely wall-blocked / unreachable targets).
- The true cell-grid `map_foreachinrange` rewrite is a **separate perf ticket** —
  this ticket's `ForEachInRange` switch is the parity-correct interim and must not
  be blocked on the grid optimization.
