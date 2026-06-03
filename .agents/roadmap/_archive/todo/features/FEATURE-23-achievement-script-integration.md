# FEATURE-23 — Achievement script integration (conditions + reward scripts)

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-04 (achievement service), SCRIPT-02 (player-state script builtins) · **Blocks:** none

## Problem

FEATURE-04 implements achievement objectives, completion, dependents, item/title rewards
and level — but two script-coupled pieces are left to the scripting epic:

1. **`Condition` scripts** (92 achievements in `achievement_db.yml`) — a boolean script gate
   (e.g. `BaseLevel >= 99`) evaluated before an achievement can progress/complete.
   `AchievementService.CheckCondition` currently returns `true` (correct for the
   *conditionless* achievements that make up the rest, but it can't yet gate the 92 that
   carry a `Condition:`).
2. **Reward `Script`** — the `Rewards.Script` bonus-script run on claim (a small rAthena
   script block). FEATURE-04 grants the reward **item + title**, but not the bonus script.

Both need the NPC/achievement script runtime, which is the deferred scripting epic.

## Current state (C#)

- `Map.Server/Achievement/AchievementService.cs` — `CheckCondition(pc, id) => true` (FEATURE-04
  doc notes this is the conditionless fallback); `GetReward` grants `RewardItem` × `RewardAmount`
  + records `RewardTitleId`, with a comment marking the bonus-script seam.
- `Core.Database/Entities/AchievementDbEntity.cs` — has `RewardItem`/`RewardAmount`/`RewardTitleId`;
  **no** `Condition` or `RewardScript` columns.
- `Tools.RathenaImporter/Converters/AchievementDbConverter.cs` — parses `Rewards.{Item,Amount,TitleId}`;
  skips `Rewards.Script` and `Condition`.

## rAthena reference (source of truth)

- `rathena/src/map/achievement.cpp` `achievement_check_condition` — runs the achievement's
  `Condition` script (`run_script`) and reads the boolean result.
- `achievement_get_reward` — `run_script(ad->rewards.script, ...)` after the item/title grant.
- `db/re/achievement_db.yml` — `Condition:` (script string) + `Rewards.Script:` (script string).

## Scope

- [ ] Add `Condition` (text) + `RewardScript` (text) columns to `AchievementDbEntity` (EF migration)
      + importer mapping + reseed.
- [ ] `CheckCondition` — when a `Condition` script is present, evaluate it via the script engine
      against the PC (boolean). No script → true (keep FEATURE-04 behavior).
- [ ] `GetReward` — after the item/title grant, run the `RewardScript` via the script engine.
- [ ] Gate objective progress/completion on `CheckCondition` for the condition-bearing achievements.

## Done criteria

- A condition-gated achievement (e.g. a `BaseLevel`-gated one) only progresses/completes when the
  condition holds; conditionless achievements are unaffected.
- A reward with a bonus script runs that script on claim (in addition to the item/title).

## Test plan

- `AchievementServiceTests` — a condition-gated achievement blocked/allowed by a stubbed condition
  evaluator; a reward script invoked once on claim.

## Notes / gotchas

- This depends on the NPC/achievement script runtime (deferred scripting epic). Keep the
  FEATURE-04 conditionless + item/title behavior as the no-script fast path.
