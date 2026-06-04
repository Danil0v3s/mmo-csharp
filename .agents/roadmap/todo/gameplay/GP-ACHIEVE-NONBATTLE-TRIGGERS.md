# GP-ACHIEVE-NONBATTLE-TRIGGERS — Non-battle achievements (level/job/zeny/…) complete from script conditions

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SCR-PLAYER (the `readparam`/`BaseLevel`/`Class` script-condition engine) · **Unlocks:** none

## The deliverable (definition of done, in one sentence)

> A player **reaching base/job level N, changing job, gaining/spending zeny, adding a friend, marrying,
> etc. completes the matching non-battle achievement** (gold check in the window), and it survives logout.

## Player story / why it matters

GP-ACHIEVE landed the entire achievement client UI (list/update/reward-ack/title), the manual reward
claim, the title equip + persistence, and the **AG_BATTLE / AG_TAMING** mob-kill trigger end-to-end.
The remaining achievement **groups** (AG_GET_ZENY, AG_SPEND_ZENY, AG_GOAL_LEVEL, AG_GOAL_STATUS,
AG_JOB_CHANGE, AG_ADD_FRIEND, AG_PARTY, AG_MARRY, AG_BABY, AG_CHATTING_*, AG_ENCHANT_*) all gate
completion on `ad->condition` — a parsed **script** snippet (e.g. `" BaseLevel >= 99 "`,
`" Class >= JOB_SWORDMAN && Class <= JOB_THIEF "`, `" readparam(bStr) >= 90 "`). rAthena's
`achievement_update_objectives` returns `false` for every one of those groups unless
`achievement_check_condition(ad->condition, sd)` evaluates true (achievement.cpp:961-1043). Without the
script runtime there is no way to evaluate the condition, so these achievements cannot complete.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Catalog | partial | `AchievementDbEntity` has Group/Targets/Score/Dependents/Reward* — **no `Condition` column** (the YAML `Condition:` script is dropped by the importer). Add the column + importer field + seed regen. |
| Trigger fan-out | ❌ | rAthena calls `achievement_update_objective(sd, AG_*, …)` from pc.cpp (level-up 8162/8216, job-change 10913, gain/spend zeny 5737/vending 217, get-item 5972, marry 12676), party.cpp:185, clif.cpp add-friend 15523. None are wired on the C# side. |
| Condition eval | ❌ | rAthena `achievement_check_condition` runs the parsed script with `ARG0..ARGn` globalregs set. Needs the SCR-PLAYER `readparam`/`BaseLevel`/`Class`/comparison runtime. |
| Objective accumulate (AG_SPEND_ZENY) | ❌ | the only group that also accumulates a target before the condition gate. |

## rAthena reference

- `src/map/achievement.cpp` — `achievement_update_objectives` group switch (the non-mob arms all require
  `ad->condition`), `achievement_check_condition`, `achievement_update_objective` (sets ARG0..ARGn).
- `src/map/pc.cpp` / `party.cpp` / `vending.cpp` / `clif.cpp` — the 12 `achievement_update_objective`
  call sites listed above.

## Scope — every layer

- [ ] **Data**: `AchievementDbEntity.Condition` column + EF migration; importer carries the YAML
  `Condition` string; seed regen.
- [ ] **Trigger fan-out**: wire `IAchievementService.UpdateObjective(group, …)` calls into the C#
  equivalents of the rAthena call sites (level-up via ExpService, job-change, zeny gain/spend, add-friend,
  party-join, marry, get-item).
- [ ] **Condition eval**: evaluate the catalog Condition via the SCR-PLAYER runtime (ARG0..ARGn bound),
  replacing the current `CheckCondition` → always-true stub.
- [ ] **AG_SPEND_ZENY accumulate**: pre-add the spent-zeny target before the condition gate.
- [ ] **Tests**: a level-up completes a `BaseLevel >= N` achievement; a job-change completes a `Class`
  range achievement; spend-zeny accumulates + completes; condition-false does NOT complete.

## Done criteria

- Reaching base level 99 completes the matching AG_GOAL_LEVEL achievement (gold check, claimable reward).
- Changing to a Swordman-line job completes the matching AG_GOAL_STATUS/Class achievement.
- A condition that isn't met leaves the achievement incomplete.
- Persists across logout (rides the existing AchievementSave round-trip).

## Notes / gotchas

- The mob-kill path (AG_BATTLE/AG_TAMING) is already done in GP-ACHIEVE — do not touch it.
- `CheckCondition` currently returns `true` (no Condition stored). Flipping it to a real evaluator must
  not regress the mob groups (they don't use a condition).
