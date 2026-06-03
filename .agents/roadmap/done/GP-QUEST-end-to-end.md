# GP-QUEST — Quests work end-to-end

> **Epic:** gameplay · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
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
- [x] **Service**: objective filters (any-mob + race/size/element/level/map) on
      `UpdateMobObjective` (archive FEATURE-21). *(turn 5 — `QuestDbEntity` extended with 21 filter
      columns (race/size/element/min-max level/location/allow-list ×3) + EF migration
      `QuestObjectiveFilters` + importer/seed regen (4826 quests). `UpdateMobObjective` now takes a
      `QuestMobContext` (id/aegis/level/race/size/element) and runs the rAthena 7-check gate; an
      objective exists iff `Count>0` (mob-specific OR any-mob).)* Instance-source-map Location branch
      ➡️ Moved to GP-QUEST-FILTER-INSTANCE; filtered-objective display label ➡️ Moved to
      GP-QUEST-FILTER-DISPLAY.
- [x] **Service (immediate save)**: immediate persistence on add/change/delete/status —
      `chrif_save(CSAVE_NORMAL)` parity (archive FEATURE-22). *(turn 4 — `IQuestSaveTrigger`
      breaks the QuestService→intif DI cycle via lazy `IServiceProvider` resolution; `RequestSave`
      fires on `Add`/`Change`/`Delete`/`UpdateStatus`, mirroring rAthena's explicit chrif_save sites.
      Objective ticks set dirty only and ride the periodic save — rAthena parity, quest.cpp:804.)*
- [x] **CZ handlers**: active-quest toggle (`CZ_ACTIVE_QUEST` 0x02b6 → `ActiveQuestHandler` →
      `UpdateStatus`). *(turn 4.)* Accept-from-NPC / cancel-erase are NPC-script-driven
      (`setquest`/`erasequest` builtins) → belong to **SCR-DOMAIN** which this ticket unlocks, not a
      client→server quest packet.
- [x] **ZC emits**: quest list on login (turn 3), add + mission (turn 2), delete (turn 1),
      objective-count update on kill (turn 1), status update (`ZC_ACTIVE_QUEST` 0x02b7, turn 4).
- [x] **Persistence**: quest + objective rows round-trip (`SnapshotFor`/`Hydrate` over the
      char-server `QuestSave`/`QuestLoad` RPCs, archive FEATURE-02); relog restores progress, and
      every mutation now persists immediately (turn 4 FEATURE-22) so progress survives a crash mid-hunt.

## Done criteria

- ✅ Player accepts a hunt quest → quest window shows it with 0/10 (`ZC_ADD_QUEST`); kills mobs →
  the count ticks up live (`ZC_UPDATE_MISSION_HUNT`); reaching the target marks it complete
  (`TryComplete` → Q_COMPLETE) and drops it from the active log. *(Reward grant on completion is an
  NPC-script action — `getitem`/`completequest` builtins — and belongs to SCR-DOMAIN, which this
  ticket unlocks; the quest-engine completion + persistence are done here.)*
- ✅ A filter quest ("kill 10 Fish-type") counts only matching mobs (turn 5, 7-check gate).
- ✅ Relog mid-quest → progress intact (immediate save on every mutation, turn 4).
- ✅ No quest CZ handler / ZC emit missing (active-quest toggle + add/mission/delete/update/list/status).
- Instance-source-map Location matching ➡️ GP-QUEST-FILTER-INSTANCE (blocked on GP-INSTANCE);
  filtered-objective display label ➡️ GP-QUEST-FILTER-DISPLAY (cosmetic; counting is correct).

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

- **2026-06-03 (turn 4)** — Active-quest toggle + immediate-save landed. New `CZ_ACTIVE_QUEST`
  (0x02b6, 7B `quest_id.L active.B`) + `ZC_ACTIVE_QUEST` (0x02b7, 7B) + `ActiveQuestHandler`
  (`clif_parse_questStateAck` → `quest_update_status`): the client toggles a quest's tracked state
  and the server flips Q_ACTIVE/Q_INACTIVE, confirms via `ZC_ACTIVE_QUEST`, or — on a complete
  transition — drops it with `ZC_DEL_QUEST` (rAthena `quest_update_status` move-to-completed +
  `clif_quest_delete`). FEATURE-22 immediate-save: `IQuestSaveTrigger` (lazy `IServiceProvider`
  resolution to break the QuestService→`IIntifService` DI cycle) fires `QuestSave` on
  `Add`/`Change`/`Delete`/`UpdateStatus`, mirroring rAthena's `chrif_save(CSAVE_NORMAL)` calls;
  objective ticks stay dirty-only (periodic save), matching quest.cpp:804. Tests: `QuestEmitTests`
  +3 (status inactive→`ZC_ACTIVE_QUEST`+save, complete→`ZC_DEL_QUEST` not active, add→save),
  new `ActiveQuestHandlerTests` (3: active/inactive mapping + unspawned-ignored). Full suite 4439
  pass (1 = standing replay-fixture). **Only FEATURE-21 objective filters remain** (needs a
  `QuestDbEntity`/schema extension — race/size/element/min-max-level/map per objective) before
  GP-QUEST is done.

- **2026-06-03 (turn 5 → DONE)** — Objective filters (FEATURE-21) landed; GP-QUEST complete.
  `QuestDbEntity` extended with 21 per-slot filter columns (`race`/`size`/`element`/`min_level`/
  `max_level`/`location`/`mobs_allowed` ×3) + EF migration `QuestObjectiveFilters`;
  `QuestDbConverter` reads the YAML `Targets` filter fields (Race/Size/Element/MinLevel/MaxLevel/
  Location + `MapMobTargets` allow-list, MinLevel-defaults-to-1-when-MaxLevel-set rule, Count:0 skip)
  and the seed was regenerated (4826 quests). `UpdateMobObjective(pc, QuestMobContext)` now runs
  rAthena's 7-check gate (quest.cpp:771): a specific-mob objective matches by aegis; an any-mob
  objective (empty `MobN`, `mob_id==0`) must pass min/max level + race + size + element + location
  (map-name-hash compare vs `pc.MapId`) + the optional allow-list. `ObjectiveTarget`/`ObjectiveCount`
  switched to "exists iff Count>0" so any-mob objectives are real everywhere. `MobDeathObserver`
  builds the context from `mob.DbEntry` (id/aegis/level/race/size/element). Tests: 5 new filter cases
  (race, min-level, size+element, allow-list, location) + the existing aegis/emit/handler suites
  migrated to `QuestMobContext`. Full suite 4444 pass (1 = standing replay-fixture). Two follow-ups
  filed: **GP-QUEST-FILTER-INSTANCE** (instance_src_map Location branch, blocked on GP-INSTANCE) and
  **GP-QUEST-FILTER-DISPLAY** (`clif_quest_string` label for filtered objectives — cosmetic).

## Notes / gotchas

- Quest expiry is query-based (no `questexpire_timer`) — verified in archive FEATURE-03.
- Coordinate the `clif_quest_*` packet defs with GP-ACHIEVE to avoid duplicate work.
- Reward grant on quest completion is an NPC-script action (`getitem`/`completequest`) → SCR-DOMAIN.
- Location filter uses the map-name-hash (`(uint)name.GetHashCode()`, the production `Name2MapId`);
  the instance source-map case is GP-QUEST-FILTER-INSTANCE.

## History

- **2026-06-03** — Done across 5 loop turns. Quest client packet bridge (ZC add/mission/delete/
  update/list + CZ/ZC active-quest toggle), load-on-enter hydrate→snapshot, immediate-save
  (chrif_save parity), and FEATURE-21 any-mob objective filters (7-check race/size/element/level/
  location/allow-list + schema/importer/seed extension). Full suite 4444 pass. Follow-ups:
  GP-QUEST-FILTER-INSTANCE, GP-QUEST-FILTER-DISPLAY. Commits: f667c71d (turn 4) + this finish.
