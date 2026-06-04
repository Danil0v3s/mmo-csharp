# AI-CHANGECHASE-VIS — changechase honours the target visibility gate

> **Epic:** mobai · **Status:** ✅ Done (2026-06-04) · **Size:** S · **Player-visible:** partial
> **Depends on:** none · **Unlocks:** none

## The deliverable

> The MD_CHANGECHASE retarget (`MobChangeTargetService.TryChangeChase`) skips an enemy the mob
> **can't currently perceive/skill-target** (hidden / cloaked / chase-failed), matching rAthena's
> `status_check_skilluse` gate inside `mob_ai_sub_hard_changechase`.

## Why / current state

AI-PERF (MOBAI-07) fixed the core changechase divergence — rAthena sets the in-reach enemy
**directly** (`md->target_id = bl->id`), bypassing `mob_can_changetarget`, and the C# now does the same
(`MobAiService` no longer gates the changechase switch with `CanChangeTarget`). The one rAthena gate
still missing: `mob_ai_sub_hard_changechase` (mob.cpp:1348) also requires
`status_check_skilluse(&md->bl, bl, 0, 0)` — i.e. the candidate must be visible/targetable (not
Hiding/Cloaking, line-of-sight ok). `TryChangeChase` currently checks only is-enemy + in-melee-reach,
so a chasing mob could changechase onto a hidden player that just stepped adjacent.

## rAthena reference

- `rathena/src/map/mob.cpp:1348 mob_ai_sub_hard_changechase` — the `status_check_skilluse` guard.
- `rathena/src/map/status.cpp:status_check_skilluse` — the hide/cloak/visibility check.

## Scope

- [x] Added the visibility/skill-use gate to `MobChangeTargetService.TryChangeChase` (new optional
      `canPerceive` predicate). `MobAiService` passes `Perceives(mob, target)` = the hide/cloak gate
      (`EntityActionGates.CanSee` — the SC-based hiding set, with boss/detector pierce) + the
      line-of-sight path check the aggressive scan already uses (`IPathService.PathSearchLong`).

## Done criteria

- ✅ A hidden/cloaked player adjacent to a chasing MD_CHANGECHASE mob is NOT changechased onto; a visible
  one still is. (`MobChangeTargetModeTests.ChangeChase_skips_a_hidden_enemy_in_reach` +
  `…switches_to_a_visible_enemy_in_reach`.)

## History

- 2026-06-04 — Done. `TryChangeChase` gained an optional `canPerceive` predicate; `MobAiService` supplies
  the `status_check_skilluse` equivalent (`CanSee` hide/cloak + LOS) so the changechase retarget skips a
  hidden / wall-blocked enemy. 2 tests; full Map.Server.Tests 4551 pass (1 = standing replay-fixture).

## Notes

- Filed by AI-PERF (MOBAI-07). The direct-set behaviour matches rAthena; this is the remaining
  visibility sub-gate.
