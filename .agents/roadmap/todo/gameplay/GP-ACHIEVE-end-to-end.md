# GP-ACHIEVE — Achievements work end-to-end

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] **CZ handlers**: request achievement list, claim reward, set title.
- [ ] **Service**: verify check/progress/reward at HEAD; wire the non-battle trigger types.
- [ ] **Triggers**: fan-out points for level/job/zeny/death/etc. → `UpdateObjective`.
- [ ] **ZC emits**: achievement list (login), update on progress, reward ack, title list.
- [ ] **Persistence**: achievement progress + rewarded-flag + earned titles round-trip.

## Done criteria

- A kill/level/job action ticks the matching achievement; completing one lets the player claim
  its item + title from the window; the title can be equipped + shows on the character.
- Rewards are idempotent (no double-grant); progress persists across logout.
- No achievement CZ handler / ZC emit missing.

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
