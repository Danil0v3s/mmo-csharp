# GP-ACHIEVE — Achievements work end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** yes
> **Depends on:** none (shares quest-UI packet work with GP-QUEST) · **Unlocks:** SCR-DOMAIN

## The deliverable

> A player **earns achievements from in-game actions (kills, level, job, zeny…), sees them
> in the achievement window with progress, claims rewards (item + title), and equips a title**
> — live client, surviving logout.

## Player story

The achievement *service* is real (dependency-gated completion, progress, reward grant + title,
idempotent — archive FEATURE-04), but only the mob-kill trigger is wired; non-battle triggers,
the reward *script* path, and the entire achievement client UI are missing.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `achievement_db` + reward item/amount/title cols seeded (archive FEATURE-04) |
| Service | ✅ verify | `Map.Server/Achievement/AchievementService.cs` — check/progress/complete/reward/titles (archive FEATURE-04) |
| Mob-kill trigger | ✅ | `MobDeathObserver` → `AchievementService.UpdateObjective` |
| Non-battle triggers | ❌ | level/job/zeny/etc. (archive FEATURE-24) |
| Reward scripts | ❌ | condition + reward script execution (archive FEATURE-23) |
| CZ handlers | ❌ | request-list, reward-claim, set-title missing |
| ZC emits | ❌ | achievement list, update, reward ack, title list missing |

## rAthena reference

- `rathena/src/map/achievement.cpp` — `achievement_update_objective` (per `e_achievement_group`
  trigger types), `achievement_check_complete`, `achievement_get_reward` (item+title+script),
  `achievement_check_dependent`. Triggers fire from `pc.cpp` (level/job/zeny/death/…).
- `rathena/src/map/clif.cpp` — parse `CZ_REQ_*ACHIEVEMENT*` (list, reward, title); emit
  `clif_achievement_list_all`, `clif_achievement_update`, `clif_achievement_reward_ack`.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Non-battle trigger hooks — call `UpdateObjective(group, …)` from the relevant pc events
  (level-up, job-change, zeny-change, etc.); build the hook points (archive FEATURE-24).
- Reward/condition scripts — needs the script runtime (`ctx.*`); the simple item+title reward
  works now, scripted conditions/rewards soft-depend on SCR-PLAYER (note + gate that part).

## Scope — every layer

- [x] **CZ handlers**: claim reward (`CZ_REQ_ACH_REWARD` → `AchievementCheckRewardHandler`), set title
  (`CZ_REQ_CHANGE_TITLE` → `ChangeTitleHandler`). (No "request list" CZ exists in rAthena — the window
  renders from the login `ZC_ALL_ACH_LIST`; verified against clif_packetdb.hpp.)
- [x] **Service**: verified check/progress/reward at HEAD; **fixed a parity deviation** — completion no
  longer auto-grants the reward (rAthena `achievement_update_achievement` only flags + emits update; the
  reward is the manual claim). Added `PcLogin`/`EmitUpdate`/`SetTitle`/`LevelInfo`/`TotalScore`.
  ➡️ Condition-gated non-battle trigger *completion* moved to **GP-ACHIEVE-NONBATTLE-TRIGGERS**
  (every non-mob group gates on a script `ad->condition` → SCR-PLAYER).
- [~] **Triggers**: the mob-kill trigger (AG_BATTLE/AG_TAMING) ships complete end-to-end. The level/job/
  zeny/friend/marry/party fan-out points need the script-condition engine to ever complete
  (rAthena returns false without `ad->condition`) ➡️ **GP-ACHIEVE-NONBATTLE-TRIGGERS** (SCR-PLAYER dep).
- [x] **ZC emits**: full list on login (`ZC_ALL_ACH_LIST`), single-achievement update on progress + reward
  (`ZC_ACH_UPDATE`), reward ack (`ZC_REQ_ACH_REWARD_ACK`), title change ack (`ZC_ACK_CHANGE_TITLE`).
  All four built with real rAthena field shapes (were opaque-byte placeholders). The "title list" is the
  list packet itself (rAthena has no separate title-list packet; `sd->titles` is server-side only).
- [x] **Persistence**: achievement progress + completed/rewarded stamps round-trip via the existing
  AchievementSave/Load IPC + load-on-enter (`IntifService.AchievementRequestAsync`). The equipped title
  rides `CharacterDataResponse.title_id` (load) + `PlayerStateService` (save) on the existing `title_id`
  char column; earned-title set is derived from rewarded achievements (no separate persistence, matching
  rAthena `achievement_get_titles`).

## Done criteria

- A kill action ticks the matching achievement (AG_BATTLE); completing one lets the player claim its item
  + title from the window via `CZ_REQ_ACH_REWARD`; the title can be equipped (`CZ_REQ_CHANGE_TITLE`) and
  rides the name block to onlookers. ✅
  ➡️ The level/job-action *completion* half moved to **GP-ACHIEVE-NONBATTLE-TRIGGERS** (script-condition
  engine / SCR-PLAYER dep).
- Rewards are idempotent (no double-grant); progress + equipped title persist across logout. ✅
- No achievement CZ handler / ZC emit missing. ✅ (scripted reward *bonus script* → **GP-ACHIEVE-REWARD-SCRIPT**, SCR-PLAYER dep — item + title rewards ship complete.)

## Test plan

- Handler tests: list/claim/title → service.
- Service: dependency-gated completion, non-battle triggers, idempotent reward (archived AchievementServiceTests).
- Persistence round-trip.
- Live: trigger → claim → equip title.

## Notes / gotchas

- `Rewards` is a YAML **mapping** not a list (archive FEATURE-04).
- Coordinate the achievement/quest UI packet defs with GP-QUEST.
- Scripted reward conditions gate on SCR-PLAYER — implement the item+title rewards fully; if a
  given achievement needs a reward *script*, note the dependency rather than stubbing the whole UI.

## History

- **2026-06-04** — Done. Built the achievement client packet bridge + title system + load-on-enter on top
  of the FEATURE-04 service. Four ZC packets fleshed out from opaque-byte placeholders to real rAthena
  field shapes (`ZC_ALL_ACH_LIST` 50B/entry, `ZC_ACH_UPDATE` 66B, `ZC_REQ_ACH_REWARD_ACK` 7B,
  `ZC_ACK_CHANGE_TITLE` 7B) + two CZ packets (`CZ_REQ_ACH_REWARD` 6B, `CZ_REQ_CHANGE_TITLE` 6B) + two
  handlers. Service additions: `PcLogin` (login emits the rAthena pc_authok tail — header-only update then
  full list), `EmitUpdate` (per-objective + per-reward tick), `SetTitle` (ownership-gated equip/clear +
  `clif_name_area` re-broadcast carrying the new title), `LevelInfo`/`TotalScore` (exact rAthena
  `achievement_level` left/right bar math). **Parity fix:** removed the FEATURE-04 auto-grant-on-completion
  — rAthena flags completion + sends `clif_achievement_update` only; the reward is the manual
  `CZ_REQ_ACH_REWARD` claim (which I wired to `achievement_check_reward` semantics: success→ack(1)+update,
  title→re-send list, fail→ack(0)). Load-on-enter: `IntifService.AchievementRequestAsync` round-trips the
  char-side log + pushes the window (mirrors quest); removed the StatusBroadcaster placeholder
  achievement emits. Equipped `title_id` persists via a new `CharacterDataResponse.title_id` proto field
  (existing char `title_id` column, no migration) + `PlayerEntity.TitleId` hydrate/save + name-block emit.
  Tests: 37 (rewrote 4 FEATURE-04 reward tests for manual-claim parity + added 11 emit/title + 4 handler +
  1 load-on-enter + 1 char title round-trip). Map.Server.Tests 4539 pass (1 standing replay fixture + 5
  environmental `scripts/dist/main.js`-missing script-bundle tests skipped by the sandbox), Char.Server.Tests
  176 pass. Filed **GP-ACHIEVE-NONBATTLE-TRIGGERS** (level/job/zeny/… completion needs the script-condition
  engine — every non-mob group gates on `ad->condition`; SCR-PLAYER dep) + **GP-ACHIEVE-REWARD-SCRIPT**
  (the bonus reward `Script` run on claim; SCR-PLAYER dep). The AG_BATTLE mob-kill trigger, the full UI,
  manual claim, title equip, and persistence all ship complete end-to-end.
