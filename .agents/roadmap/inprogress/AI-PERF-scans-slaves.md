# AI-PERF — Cell-grid range scans + full slave coupling

> **Epic:** mobai · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** partial
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

- [ ] Replace the mob-scan full-sweep with the cell-grid range query (perf, behaviour-equal).
- [ ] Complete the player-mastered slave coupling.
- [ ] Reconcile the changechase `CanChangeTarget` gate with rAthena's direct target-set.

## Done criteria

- Mob scans use the cell-grid query (measurable perf win, same behaviour); player-summoned slaves
  follow + inherit targets; changechase matches rAthena; tests pin the behaviour.

## Test plan

- Extend the archived MOBAI-03/04 tests + a scan-equivalence test.

## Notes

- Parallel. Builds on the landed slave coupling + LOS gate (archive MOBAI-01/03/04).
