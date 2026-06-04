# AI-CHANGECHASE-VIS — changechase honours the target visibility gate

> **Epic:** mobai · **Status:** ❌ Not started · **Size:** S · **Player-visible:** partial
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

- [ ] Add the visibility/skill-use gate to `MobChangeTargetService.TryChangeChase` (reuse the existing
      hide/cloak SC checks + the LOS path the aggressive scan already uses).

## Done criteria

- A hidden/cloaked player adjacent to a chasing MD_CHANGECHASE mob is NOT changechased onto; a visible
  one still is. Test pins both.

## Notes

- Filed by AI-PERF (MOBAI-07). The direct-set behaviour matches rAthena; this is the remaining
  visibility sub-gate.
