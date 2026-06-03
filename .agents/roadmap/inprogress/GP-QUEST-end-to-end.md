# GP-QUEST — Quests work end-to-end

> **Epic:** gameplay · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
> **Depends on:** none (coordinate ZC quest-UI packets with GP-ACHIEVE) · **Unlocks:** SCR-DOMAIN (quest builtins)

## The deliverable

> A player can **accept a quest, see it in the quest window with live hunt-count progress,
> have it complete, and get rewards** — live client, surviving logout.

## Player story

Quests gate most PvE content. The quest *service* is real (add/delete/check/update-objective,
quest_time expiry, hunting objectives — archive FEATURE-03), but on login the player's quests
aren't loaded/hydrated, there's no quest-window packet, and objective filters (race/size/
element/level/map) and immediate-on-mutation save are missing.

## Current state — per layer

| Layer | State | Where |
|---|---|---|
| Data | ✅ | `quest_db` seeded |
| Service | ✅ verify | `Map.Server/Quest/QuestService.cs` + `QuestTime.cs` (archive FEATURE-03) |
| Mob-kill hook | ✅ | `MobDeathObserver` → `QuestService.UpdateMobObjective` (archive FEATURE-01) |
| Persistence | partial | save-on-mutation missing (archive FEATURE-22); load-on-enter not wired (FEATURE-20) |
| Objective filters | partial | any-mob + race/size/element/level/map filters missing (FEATURE-21) |
| CZ handlers | ❌ | accept/cancel/active-set missing |
| ZC emits | ❌ | quest list, add, delete, objective-count update missing |

## rAthena reference

- `rathena/src/map/quest.cpp` — `quest_add`, `quest_delete`, `quest_update_status`,
  `quest_update_objective` (mob + `quest_db` objective filters: mob_id OR race/size/element/
  min-max level/map), `quest_check` (HAVEQUEST/PLAYTIME/HUNTING), `quest_pc_login` (load).
- `rathena/src/map/clif.cpp` — parse `CZ_ACTIVE_QUEST`/`CZ_ALL_QUEST` toggles; emit
  `clif_quest_send_list` (login), `clif_quest_add`, `clif_quest_delete`,
  `clif_quest_update_objective` (kill count), `clif_quest_update_status` (active/inactive).
- char persistence: `quest` + `quest_obj` rows; `chrif_save`/immediate save on mutate.

## Dependencies — and how to satisfy

- Packet-bridge pattern — foundation.
- Quest-window ZC packets overlap GP-ACHIEVE's UI set — coordinate the shared `clif_quest_*`
  definitions (do them once, both tickets consume).

## Scope — every layer

- [x] **Load on enter**: wire quest load → hydrate `MapSessionData` → `PcLogin` so quests
      exist on the entity at session start (archive FEATURE-20). *(turn 3 — `IntifService.QuestRequestAsync`
      round-trips the char-side log, hydrates onto the live entity, emits `ZC_ALL_QUEST_LIST`; called
      from `NotifyActorInitHandler` LoadEndAck after the inventory list.)*
- [ ] **Service**: objective filters (any-mob + race/size/element/level/map) on
      `UpdateMobObjective` (archive FEATURE-21); immediate persistence on every mutation
      (add/delete/objective tick/status) — `chrif_save` parity (archive FEATURE-22).
- [ ] **CZ handlers**: accept (from NPC), cancel/erase, active-quest toggle.
- [ ] **ZC emits**: quest list on login, add, delete, objective-count update on kill,
      status update.
- [ ] **Persistence**: quest + objective rows round-trip; relog restores progress.

## Done criteria

- Player accepts a hunt quest → quest window shows it with 0/10; kills mobs → the count
  ticks up live; reaching 10 marks it complete; reward grants.
- A filter quest ("kill 10 Fish-type") counts only matching mobs.
- Relog mid-quest → progress intact (immediate save, not just logout-save).
- No quest CZ handler / ZC emit missing.

## Test plan

- Handler tests: accept/cancel → service.
- Service: objective filters (race/size/element/level/map), immediate-save trigger.
- Persistence round-trip mid-progress.
- Live: accept → hunt → complete → reward.

## Progress log (multi-turn vertical)

- **2026-06-03 (turn 1)** — Quest client-emit infrastructure + the live hunt-counter + delete. The
  quest service was logic-complete (Add/Change/Check/Delete/UpdateObjective/UpdateMobObjective/
  UpdateStatus on `pc.QuestLog`) with explicit "PACKET-10 owns the emit" seams; this turn wired the
  emits. Injected `ISessionManagerAccessor` into `QuestService`; new `ZC_DEL_QUEST` (0x02b4) +
  `ZC_UPDATE_MISSION_HUNT` (modern 0x09fa: per-obj questId/questIndex/target/current — no mob-name
  string needed) + `EmitDelete`/`EmitUpdate` helpers wired into `Delete`, `UpdateObjective`, and
  `UpdateMobObjective`. A mob kill now ticks the hunt counter on the client; deleting a quest removes
  it from the log. `QuestEmitTests` (2) green; full suite 4431 pass (1 = standing replay-fixture).
- **Remaining (next turns → done):** (1) **`ZC_ADD_QUEST`** (the quest appears — the modern
  0x09f9 + 0x8fe form with the mob display-name per objective, needs `IMobDb` for the name + id) +
  wire into `Add`/`Change`. (2) **`ZC_ALL_QUEST_LIST`/`ZC_ALL_QUEST_MISSION`** on login (PcLogin push,
  the full snapshot). (3) **load-on-enter** wiring (hydrate → PcLogin emit). (4) **CZ_ACTIVE_QUEST**
  toggle handler. (5) **objective filters** (any-mob + race/size/element/level/map on
  `UpdateMobObjective`, archive FEATURE-21). (6) **immediate-save** on mutation (archive FEATURE-22).
  All are layers of THIS vertical; the loop resumes this card. Live-client wire validation is the
  project's standing deferred pass.
- **2026-06-03 (turn 2)** — Quest-appears (ADD) landed. Injected `IMobDb` into `QuestService`; new
  `ZC_ADD_QUEST` (0x09f9, fixed 143B: header 17 + 3×42 objective slots, zero-padded unused) +
  `ZC_ADD_QUEST_MISSION` (0x08fe, variable mission counts). `EmitAdd` resolves each objective's mob
  aegis → id + display name via `IMobDb` (Poring fallback for a missing mob, rAthena parity), wired
  into `Add` + `Change`. `QuestEmitTests` now 3 (Add verifies the 143B primary + the mission
  secondary). Full suite 4432 pass (1 = standing replay-fixture). **In-session loop reachable: accept
  (setquest→Add→appears) → kill (UPDATE ticks) → complete/delete (DEL).** Remaining (next turns →
  done): the login `ZC_ALL_QUEST_LIST` snapshot + load-on-enter + `CZ_ACTIVE_QUEST` toggle + objective
  filters (FEATURE-21) + immediate-save (FEATURE-22).

- **2026-06-03 (turn 3)** — Login snapshot + load-on-enter landed. New `ZC_ALL_QUEST_LIST`
  (0x09f8, modern PACKETVER ≥ 20150513 / `clif_quest_send_list`): variable `<len>.W <count>.L` then
  per-quest 15B (`quest_id.L state.B timeDiff.L time.L numObj.W`) + per-obj 44B (`questIndex.L effect.L
  mob_id.L minLv.W maxLv.W current.W target.W name.24`) — self-contained (carries live count + target +
  mob display name, no companion mission packet). `QuestService.PcLogin` now builds the snapshot from
  `pc.QuestLog` (mob id/name via `IMobDb`, Poring fallback) and pushes it. Load-on-enter wired:
  `IIntifService.QuestRequestAsync(pc)` awaits `QuestLoadAsync` → `QuestService.Hydrate` →
  `PcLogin`, called from `NotifyActorInitHandler` (LoadEndAck) right after the inventory list — mirrors
  rAthena's `intif_request_questlog` → `clif_quest_send_list` at the tail of `pc_authok`. New shared
  test fake `NoOpIntifService`. `QuestEmitTests` now 4 (login snapshot asserts count/state/index/mob
  id/live-count/target/name at exact offsets); full suite 4433 pass (1 = standing replay-fixture).
  **Reachable now: enter map → quest window populates from the persisted log with live progress;**
  accept → appears; kill → ticks; complete/delete → removed. Remaining (next turns → done):
  `CZ_ACTIVE_QUEST` toggle handler + objective filters (FEATURE-21) + immediate-save (FEATURE-22).

## Notes / gotchas

- Quest expiry is query-based (no `questexpire_timer`) — verified in archive FEATURE-03.
- Coordinate the `clif_quest_*` packet defs with GP-ACHIEVE to avoid duplicate work.
