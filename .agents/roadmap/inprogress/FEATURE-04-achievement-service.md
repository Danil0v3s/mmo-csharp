# FEATURE-04 — Achievement service

> **Epic:** Gameplay-Achievement · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
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

- [ ] `UpdateObjective(pc, type, ...args)` — **the FEATURE-01 hook for `AG_BATTLE`** and the general trigger: walk catalog achievements of `type`, match objective targets (mob_id for AG_BATTLE), increment the PC's `Counts[]`, complete + reward when all targets met + dependencies satisfied, emit ZC_ACH_UPDATE.
- [ ] `UpdateObjectiveSub` — per-entry increment + completion check helper.
- [ ] `CheckCondition` — evaluate the catalog `Condition` script against the PC (route through the existing Jint/script engine; if a condition has no script, treat as always-true).
- [ ] `CheckDependent` — all `Dependent[]` achievement ids completed for the PC.
- [ ] `CheckProgress` — return current progress (sum/min across objectives) for the achievement.
- [ ] `UpdateAchievement(pc, id, completed)` — mark complete (set `CompletedUnix`), recompute level, emit update.
- [ ] `Remove(pc, id)` — drop from the PC log.
- [ ] `CheckReward` / `GetReward` — manual claim path: validate completed && unrewarded, grant item + title + run reward script, set `RewardedUnix`, emit reward ack.
- [ ] `GetTitles(pc)` — return the list of title ids the PC has earned (from completed+rewarded achievements with a `Title:` field).
- [ ] `Level(pc)` — total score → achievement level.
- [ ] `MobExists(mobId)` — back it with the real mob_db (inject `IMobDb`), for AG_BATTLE target validation. (Currently `=> false`, which would reject every battle objective.)
- [ ] `Free(pc)` — clear per-PC achievement runtime on logout (keep persisted log untouched; this is the in-memory free).
- [ ] **Save**: via FEATURE-02 fan-out (`SnapshotFor` already real).
- [ ] **Client packets**: ZC_ALL_ACH_LIST (login push), ZC_ACH_UPDATE (objective/complete), ZC_REQ_ACH_REWARD_ACK (reward claim). Define/handle in `Map.Server` or call the PACKET-* emit seam — state mutation must happen here regardless.
- [ ] **Login push**: after `Hydrate`, push the achievement list to the client.

## Done criteria

- Killing a mob (via FEATURE-01) advances any `AG_BATTLE` achievement whose target mob matches; a battle achievement completes when its target count is hit, with dependencies enforced.
- A completed achievement with auto-reward grants its item/title/score; a manual-claim achievement grants only after `GetReward`.
- `GetTitles` returns the earned titles; `Level` reflects accumulated score.
- `MobExists` returns true for real mob ids (no longer a blanket false).
- `SnapshotFor` after triggers reflects updated counts/completed/rewarded, and survives save→relog.
- No `=> false` / `=> 0` / empty gameplay method left in `AchievementService`.

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
