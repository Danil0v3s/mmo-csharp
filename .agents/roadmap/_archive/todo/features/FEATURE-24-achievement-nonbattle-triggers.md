# FEATURE-24 — Non-battle achievement trigger callsites (level / job / zeny / etc.)

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-04 (achievement objective engine) · **Blocks:** none

## Problem

FEATURE-04 made `AchievementService.UpdateObjective` real for the **AG_BATTLE** group (the
FEATURE-01 mob-kill path). But rAthena fires `achievement_update_objective` from many other
subsystems, and those callsites don't exist in the C# port yet, so non-combat achievements never
advance:

- **AG_GOAL_LEVEL** — on base/job level-up.
- **AG_JOB_CHANGE** — on job change.
- **AG_GOAL_STATUS** — on a stat reaching a threshold.
- **AG_SPEND_ZENY** / **AG_GET_ZENY** — on zeny spend/gain.
- **AG_CHATTING** / **AG_ADD_FRIEND** / **AG_PARTY** / **AG_MARRY** / **AG_TAMING** / **AG_EAT** /
  **AG_REFINE_SUCCESS** / **AG_REFINE_FAIL** — on the respective subsystem events.

## Current state (C#)

- `Map.Server/Achievement/AchievementService.cs UpdateObjective` — handles only the mob-keyed groups
  (AG_BATTLE / AG_TAMING target matching). The other groups parse no targets and have no caller.
- The triggering subsystems (level-up in `ExpService`/status, job change, zeny ops, refine, party,
  chat) do **not** call `IAchievementService.UpdateObjective`.

## rAthena reference (source of truth)

- `rathena/src/map/achievement.cpp achievement_update_objective` — the per-group `case` arms
  (AG_GOAL_LEVEL reads `status_get_lv`, AG_SPEND_ZENY accumulates, etc.).
- The callsites: `pc.cpp` (level-up, job change, zeny), `clif.cpp` (chatting), `party.cpp`, etc.

## Scope

- [ ] Extend the achievement objective schema/parse to carry the non-mob target conditions per group
      (level threshold, job id, stat+value, zeny amount, …) — likely a richer objective structure
      (overlaps FEATURE-21's quest-objective work).
- [ ] Implement the per-group matching in `UpdateObjective` for each non-battle group.
- [ ] Add the trigger callsites in the owning subsystems (level-up, job-change, zeny, refine, party,
      chat) — each calls `UpdateObjective(group, …)`.

## Done criteria

- Reaching a level completes an AG_GOAL_LEVEL achievement; changing job completes an AG_JOB_CHANGE
  one; spending the target zeny completes an AG_SPEND_ZENY one — each matching rAthena.

## Test plan

- `AchievementServiceTests` — one representative per group (level, job, zeny) advancing/completing.
- Integration: the level-up / job-change / zeny callsites fire the update.

## Notes / gotchas

- The objective schema for non-mob groups is the main lift; coordinate with FEATURE-21 (quest filter
  objectives) since both want a richer objective representation than the flattened columns.
