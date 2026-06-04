# AI-PERF — Cell-grid range scans + full slave coupling

> **Epic:** mobai · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** partial
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Mob AI scans use a true cell-grid range query (not a full-map sweep), and the slave/master
> coupling is complete (including player-mastered slaves and the changechase direct-set).

## What this absorbs (archive)

- `_archive/todo/mobai/MOBAI-05` — true cell-grid range query for the mob scans (perf).
- `_archive/todo/mobai/MOBAI-06` — slave-coupling pass for engaged (player-mastered) slaves.
- `_archive/todo/mobai/MOBAI-07` — changechase target-set: reconcile CanChangeTarget gate with rAthena's direct set.

## rAthena reference

- `rathena/src/map/mob.cpp` — `mob_ai_sub_hard` scan (`map_foreachinrange` cell-grid), the
  slave→master follow/target-inherit, `mob_changetarget` direct set.

## Scope

- [x] **Cell-grid range scans** — verified done: every mob-AI *range* scan already routes through the
      bucketed cell-grid (`EntityRegistry.ForEachInRange` → `MapSpatialIndex`, the (2·range+1)² cell
      walk): the aggressive aggro scan, `TryChangeChase`, `PickRandomEnemy`, `RetargetMobsChasing`, and
      the slave friend-search (landed in MOBAI-03). The only remaining `_entities.All()` walks are the
      per-tick mob/summon dispatch loops and `CountSlaves`/spotted-cleanup — entity-set iterations, not
      range scans (`CountSlaves` is a documented amortised O(N) the class flags for a later master-keyed
      index; not in scope here).
- [x] **Player-mastered slave coupling** — completed: `SlaveMobService.TickSlave` target inheritance now
      inherits the master's target whatever its type (was PlayerEntity-only), so a player-summoned slave
      joins its master against a MOB target too (rAthena `mob_ai_sub_hard_slavemob`). Follow / stray-and-
      return (>5 cells from a player master) / die-with-master were already in place (MOBAI-01).
- [x] **Changechase direct set** — fixed: `mob_ai_sub_hard_changechase` (mob.cpp:1348) sets the in-reach
      enemy **directly** (`md->target_id = bl->id`) without `mob_can_changetarget`; `MobAiService` no
      longer gates the changechase switch with `CanChangeTarget`, so a RUSH-state MD_CHANGECHASE mob
      switches even without MD_CHANGETARGETCHASE. The remaining `status_check_skilluse` visibility
      sub-gate ➡️ **AI-CHANGECHASE-VIS**.

## Done criteria

- ✅ Mob *range* scans use the cell-grid query (same behaviour — already in place since MOBAI-03; no
  remaining range-scan full-sweep).
- ✅ Player-summoned slaves follow + inherit targets of any type
  (`MobAiServiceTests.Idle_slave_inherits_a_non_pc_master_target` + the existing follow/inherit tests).
- ✅ Changechase matches rAthena's direct set
  (`MobChangeTargetModeTests.ChangeChase_in_rush_switches_directly_without_the_changetargetchase_bit`);
  the `status_check_skilluse` visibility sub-gate ➡️ **AI-CHANGECHASE-VIS**.

## History

- 2026-06-04 — Done. Mostly a verify ticket (the cell-grid range scans + most slave coupling landed in
  MOBAI-01/03/04). Two real fixes: the changechase now sets the in-reach enemy directly (no
  `CanChangeTarget` gate — MOBAI-07), and slave target-inheritance accepts any-type master target
  (MOBAI-06). 2 regression tests; full Map.Server.Tests 4549 pass (1 = standing replay-fixture). Filed
  AI-CHANGECHASE-VIS (the changechase visibility sub-gate).

## Test plan

- Extend the archived MOBAI-03/04 tests + a scan-equivalence test.

## Notes

- Parallel. Builds on the landed slave coupling + LOS gate (archive MOBAI-01/03/04).
