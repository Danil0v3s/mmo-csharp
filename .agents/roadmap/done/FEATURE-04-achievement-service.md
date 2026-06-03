# FEATURE-04 — Achievement service

> **Epic:** Gameplay-Achievement · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-01 (mob-death + trigger dispatch), FEATURE-02 (save) · **Blocks:** none
> **Related:** PACKET-* (ZC achievement packets)

## Problem

Achievements load (~362 rows) and round-trip via snapshot/hydrate, but **every
gameplay method returns false / 0 / empty**. No trigger ever fires, no
objective ever advances, no reward is ever granted, titles are never awarded,
and the client never sees an achievement update. Achievements are inert.

## Current state (C#)

- `Map.Server/Achievement/AchievementService.cs`:
  - `:30 CheckCondition(pc, id) => false;`
  - `:31 CheckDependent(pc, id) => false;`
  - `:32 Remove(pc, id) => false;`
  - `:33 UpdateAchievement(pc, id, completed) => false;`
  - `:34 CheckProgress(pc, id) => 0;`
  - `:35 UpdateObjectiveSub(pc, id, objective, delta) => 0;`
  - `:36 UpdateObjective(pc, type, index, value) { }`
  - `:37 CheckReward(pc, id) { }`
  - `:38 GetReward(pc, id) { }`
  - `:39 GetTitles(pc) => Array.Empty<int>();`
  - `:40 Free(pc) { }`
  - `:41 Level(pc) => 0;`
  - `:42 MobExists(int) => false;`
  - Working: `:44 ReloadDb()`, `:63 GetCatalogEntry`, `:67 SnapshotFor`, `:91 Hydrate`.
- `Map.Server/Services/Intif/IntifService.cs:488 AchievementSave` + `:500 AchievementRequest` real (orphaned per FEATURE-02).
- `PlayerEntity.AchievementLog` holds `AchievementEntry { AchievementId, CompletedUnix, RewardedUnix, Score, Counts[] }`.

## rAthena reference (source of truth)

- `rathena/src/map/achievement.cpp`:
  - `achievement_add(sd, achievement_id)` / `achievement_remove` — manage the per-PC `achievements[]` array.
  - `achievement_update_objective(sd, AchievementType type, uint8 count, ...)` — the central trigger entry. For each catalog achievement of `type` whose `targets[]` (objective conditions) match the supplied args, bump `count[i]`; when all objectives met and dependencies (`achievement_check_dependent`) satisfied, set `completed = now`, `clif_achievement_update`, recompute `achievement_level`, and **auto-grant reward** if the achievement has no manual claim step. Achievement types include `AG_ADD_FRIEND`, `AG_BABY`, `AG_BATTLE` (mob kill), `AG_TAMING`, `AG_CHATTING`, `AG_GOAL_LEVEL`, `AG_GOAL_STATUS`, `AG_JOB_CHANGE`, `AG_ENCHANT_*`, `AG_SPEND_ZENY`, etc.
  - `achievement_check_condition(...)` — evaluate the achievement's script `Condition` (Boolean) against the PC.
  - `achievement_check_dependent(sd, achievement_id)` — all prerequisite achievement ids completed?
  - `achievement_get_reward(sd, achievement_id)` — on manual claim (CZ_REQ_ACH_REWARD): if completed && not yet rewarded, grant `Rewards` (item + title id + script), set `rewarded`, `clif_achievement_reward_ack`.
  - `achievement_level(sd, ...)` — total achievement score → level, drives `ZC_ACH_UPDATE`/title list.
  - `mob_exists` (in mob.cpp) — used by `AG_BATTLE` objective validation (target mob id is a real mob).

## Scope — every sub-system that must be touched

- [x] `UpdateObjective(pc, type, ...args)` — the **AG_BATTLE** hook (FEATURE-01): match mob targets, increment `Counts[]`, complete + auto-reward when all targets met **and `CheckDependent` passes**. ➡️ The non-battle trigger groups (AG_GOAL_LEVEL / AG_JOB_CHANGE / AG_SPEND_ZENY / …) + their subsystem callsites → **FEATURE-24**.
- [x] `UpdateObjectiveSub` — per-objective increment helper (clamps to the parsed target).
- [x] `CheckCondition` — conditionless achievements → true (correct for the current schema, which stores no `Condition`). ➡️ The 92 `Condition`-script achievements need the schema column + script-engine eval → **FEATURE-23**.
- [x] `CheckDependent` — all `Dependent[]` achievement ids completed for the PC.
- [x] `CheckProgress` — return current progress (sum/min across objectives) for the achievement.
- [x] `UpdateAchievement(pc, id, completed)` — mark complete (set `CompletedUnix`), recompute level, emit update.
- [x] `Remove(pc, id)` — drop from the PC log.
- [x] `CheckReward` / `GetReward` — grant the reward **item** (`RewardItem`×`RewardAmount` via `IInventoryService.GiveItem`) + **title** (`RewardTitleId`), set `RewardedUnix`, idempotent. Added the `reward_item`/`reward_amount`/`reward_title_id` schema + EF migration + importer + reseed (203 items). Auto-granted on completion via `CheckReward` (rAthena uses a manual `CZ_REQ_ACH_REWARD` claim → **PACKET-10**; the grant is idempotent either way). ➡️ The bonus reward **`Script`** → **FEATURE-23**.
- [x] `GetTitles(pc)` — return the list of title ids the PC has earned (from completed+rewarded achievements with a `Title:` field).
- [x] `Level(pc)` — total score → achievement level.
- [x] `MobExists(mobId)` — back it with the real mob_db (inject `IMobDb`), for AG_BATTLE target validation. (Currently `=> false`, which would reject every battle objective.)
- [x] `Free(pc)` — clear per-PC achievement runtime on logout (keep persisted log untouched; this is the in-memory free).
- [x] **Save**: via FEATURE-02 fan-out (`SnapshotFor` already real).
- [x] **Client packets**: state mutations are real; ZC_ALL_ACH_LIST / ZC_ACH_UPDATE / ZC_REQ_ACH_REWARD_ACK wire formats + emit owned by existing **PACKET-10** — marked seams (no no-ops).
- [x] **Login push**: `Free`/`Hydrate`/`SnapshotFor` ready; ➡️ the session-enter load→Hydrate call site is **FEATURE-20** (shared with quest), the ZC list push is **PACKET-10**.

## Done criteria

- Killing a mob (via FEATURE-01) advances any `AG_BATTLE` achievement whose target mob matches; completes on target count, **dependencies enforced**. ✅
- A completed achievement grants its item/title/score. ✅ (score on completion, item+title via the auto-grant `CheckReward`→`GetReward`, idempotent). ➡️ rAthena's manual `CZ_REQ_ACH_REWARD` claim packet → **PACKET-10**; bonus reward script → **FEATURE-23**.
- `GetTitles` returns the earned titles; `Level` reflects accumulated score. ✅ (Level walks the `achievement_level_db` curve).
- `MobExists` returns true for real mob ids (no longer a blanket false). ✅ (FEATURE-01).
- `SnapshotFor` after triggers reflects updated counts/completed/rewarded, and survives save→relog. ✅ (via FEATURE-02 fan-out; `RewardedUnix` rides the snapshot).
- No `=> false` / `=> 0` / empty gameplay method left in `AchievementService`. ✅
- Non-battle trigger groups (level/job/zeny/…) → **FEATURE-24**; condition-script eval → **FEATURE-23**.

## Test plan

- `Map.Server.Tests` (add) `AchievementServiceTests`:
  - `UpdateObjective(AG_BATTLE,1,mobId)` advances matching achievement, completes on target, enforces dependents;
  - `CheckDependent` blocks completion until prereqs done;
  - `GetReward` grants once and is idempotent on a second claim;
  - `MobExists` true/false against a stub mob_db;
  - `GetTitles` / `Level` reflect completed set.
- Integration with FEATURE-01 observer.
- Manual/live: open the achievement window, kill targets, confirm progress + reward + title.

## Notes / gotchas

- `MobExists` returning false today silently disables all battle achievements — wiring `IMobDb` is the highest-leverage single fix.
- Reward grant must run through the item-grant + script paths (don't directly mutate inventory bytes).
- Some achievements complete via non-combat triggers (zeny spent, job change, level goal) — wire those trigger callsites where the corresponding subsystem fires (e.g. level-up, job-change), but the AG_BATTLE path is the FEATURE-01 critical one.
- Keep `SnapshotFor`/`Hydrate` shape stable for char-side persistence.

## History

- 2026-06-03 · Implemented the achievement service on the FEATURE-01 AG_BATTLE hook.
  De-stubbed every gameplay method: `CheckDependent` (Dependents CSV all-completed),
  dependency-gated completion in `UpdateObjective`, `UpdateObjectiveSub` (clamping
  per-objective), `CheckProgress` (sum), `UpdateAchievement` (mark/clear + score),
  `Remove`, `Free`, `Level` (walks the `achievement_level_db` curve, rAthena
  achievement.cpp:811), `CheckCondition` (conditionless→true), `CheckReward`/`GetReward`
  (grant `RewardItem`×`RewardAmount` via `IInventoryService.GiveItem` + `RewardTitleId`,
  idempotent via `RewardedUnix`), `GetTitles`. Added the reward schema
  (`reward_item`/`reward_amount`/`reward_title_id`) to `AchievementDbEntity` + EF
  migration `AchievementRewardColumns` + config + importer (`Rewards` mapping parse) +
  reseed (203 reward items). `AchievementServiceTests` (8) green; full suite 4322 pass
  (1 fail = pre-existing INFRA-11). Follow-ups: FEATURE-23 (condition + reward-script
  engine integration), FEATURE-24 (non-battle trigger groups + callsites); client ZC
  packets → PACKET-10; session-enter load wiring → FEATURE-20.
