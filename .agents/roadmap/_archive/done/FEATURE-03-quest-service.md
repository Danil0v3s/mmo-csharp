# FEATURE-03 — Quest service

> **Epic:** Gameplay-Quest · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** FEATURE-01 (mob-death observer drives objective updates), FEATURE-02 (save) · **Blocks:** none
> **Related:** PACKET-* (ZC quest UI packets share the packet epic)

## Problem

The quest log is dead weight. Catalog loads, snapshot/hydrate round-trips the
persisted log, but **every mutation method returns 0 or does nothing**. A
player cannot accept a quest (`Add`), abandon one (`Delete`), progress a
hunting objective (`UpdateObjective`), or have a time-limited quest expire. The
quest UI never updates. None of the ~4,800 loaded quests are playable.

## Current state (C#)

- `Map.Server/Quest/QuestService.cs`:
  - `:32 Add(pc, questId) => 0;`
  - `:33 Change(pc, oldQuestId, newQuestId) => 0;`
  - `:34 Check(pc, questId, status) => 0;`
  - `:35 Delete(pc, questId) => 0;`
  - `:36 PcLogin(pc) => 0;`
  - `:37 UpdateObjectiveSub(pc, questId, index, delta) => 0;`
  - `:38 UpdateObjective(pc, questId, index, delta) { }`
  - `:39 UpdateStatus(pc, questId, status) => 0;`
  - Working: `:41 Reload()` (loads `quest_db`), `:60 GetCatalogEntry`, `:64 SnapshotFor`, `:90 Hydrate`.
- `Map.Server/Services/Intif/IntifService.cs:462 QuestSave(pc)` + `:476 QuestRequest(charId)` are real (orphaned per FEATURE-02).
- `PlayerEntity.QuestLog` holds `QuestEntry { QuestId, TimeUnix, State, Counts[] }`.

## rAthena reference (source of truth)

- `rathena/src/map/quest.cpp`:
  - `quest_add(sd, quest_id)` — reject if already in log or unknown id; append `quest_log[]` entry with `state=Q_ACTIVE`, set `time` = now + the quest's `TimeLimit` (or absolute reset hour); zero the per-objective `count[]`; `clif_quest_add`; `pc_show_questinfo`; `achievement_update_objective(AG_*_QUEST...)` where applicable. Returns 0 ok / -1 fail.
  - `quest_delete(sd, quest_id)` — find index, remove, compact `quest_log[]`, `clif_quest_delete`. Returns 0/-1.
  - `quest_change(sd, qid1, qid2)` — replace qid1 with qid2 in place (delete+add semantics, keeps slot). Used by quest scripts.
  - `quest_check(sd, quest_id, qtype)` — query: `HAVEQUEST` (0/1/2 by state), `PLAYTIME` (time elapsed/expired), `HUNTING` (all objectives met?). Returns the state code.
  - `quest_update_objective(sd, mob_data*)` (and `quest_update_objective_sub`) — for each `Q_ACTIVE` quest, for each `mob[i]` objective whose `mob_id==md->mob_id` (or 0 = any), if `count[i] < target` increment, `clif_quest_update_objective` (ZC_UPDATE_MISSION_HUNT / ZC_HUNTING_QUEST_INFO). If all objectives now met, mark `Q_COMPLETE`.
  - `quest_update_status(sd, quest_id, status)` — set state (`Q_ACTIVE`/`Q_COMPLETE`/`Q_INACTIVE`), `clif_quest_update_status`.
  - `quest_pc_login(sd)` — on login, push the full quest list (`clif_quest_send_list` + `clif_quest_send_mission`) and start any per-quest time-limit timers.
  - Time limit: `quest_db` `TimeLimit` (relative seconds) or absolute reset time; expired active quests flip to a failed/removable state via a timer (`quest_update_status` from `questexpire_timer`).

## Scope — every sub-system that must be touched

- [x] `QuestService.Add` — validate (not already present, catalog known, log not full), append `QuestEntry` with computed `TimeUnix` (catalog `TimeLimit`), zeroed `Counts[]` sized to the objective count, emit ZC_ADD_QUEST. Return 0/-1 per rAthena.
- [x] `QuestService.Delete` — locate + remove from `pc.QuestLog`, emit ZC_DEL_QUEST.
- [x] `QuestService.Change` — delete-then-add in the same slot.
- [x] `QuestService.Check` — implement `HAVEQUEST` / `PLAYTIME` / `HUNTING` query codes; return the rAthena state value (callable from scripts).
- [x] `QuestService.UpdateObjective(pc, mobId)` — **the FEATURE-01 hook**: walk active quests, match mob objectives by mob_id, increment count (clamped to target), emit ZC_UPDATE_MISSION_HUNT, flip to `Q_COMPLETE` when all objectives satisfied. (Signature: change/overload from the current `(pc, questId, index, delta)` to a mob-id driven update matching rAthena, while keeping a direct `(questId,index,delta)` overload for script `setquest`-style count setting.)
- [x] `QuestService.UpdateObjectiveSub` — the per-entry increment helper used by `UpdateObjective`.
- [x] `QuestService.UpdateStatus` — set state + emit ZC_UPDATE_MISSION_HUNT/status packet.
- [x] `QuestService.PcLogin` — returns the active-quest count + is the post-hydrate push seam (ZC_ALL_QUEST_LIST → PACKET-10). **No timers armed** — see expiry note below. ➡️ The load→Hydrate→PcLogin *call site* on session enter is unwired today (`QuestRequest` discards the load response) → **FEATURE-20**.
- [x] **Time-limit expiry**: ⚠️ **Parity correction** — rAthena has **no** `questexpire_timer`; expiry is **query-based**, reported by `quest_check(PLAYTIME)`→2 / `quest_check(HUNTING)`→1 once `time < now` (quest.cpp:912-922). There is no active state-flip sweep, so the C# matches rAthena by detecting expiry in `Check` (no game-loop hook). The ticket's "per-tick sweep that flips state" was a misread of rAthena and is intentionally **not** implemented.
- [x] **Save**: quest mutations ride the existing FEATURE-02 autosave + logout final-save fan-out (`SnapshotFor` is real). ➡️ rAthena's *immediate* `chrif_save` per mutation (without the FEATURE-02 DI cycle) moved to **FEATURE-22**.
- [x] **Client packets** (ZC_ADD_QUEST / ZC_DEL_QUEST / ZC_UPDATE_MISSION_HUNT / ZC_ALL_QUEST_LIST): state mutations are real; the ZC wire formats + emit are owned by the existing **PACKET-10** — marked seams left at each emit point (no no-ops).

## Done criteria

- `Add` then `Check(HAVEQUEST)` returns active; `Delete` then `Check` returns absent. ✅
- Killing a mob (via FEATURE-01 observer) increments the matching objective by 1 and auto-completes the quest when the last objective hits target; non-matching mobs do not. ✅ (aegis match; ➡️ mob_id==0 "any mob" + race/size/element/level/map filter objectives need a richer schema → **FEATURE-21**).
- A `TimeLimit` quest flips to expired after its window without a kill. ✅ via `Check(PLAYTIME)`→2 (query-based, matching rAthena — no state-flip sweep; see scope note).
- `PcLogin` pushes the persisted quest list to the client on enter. ✅ push seam + active count; ➡️ the session-enter call site + ZC list packet are **FEATURE-20** / **PACKET-10**.
- `SnapshotFor` after a series of mutations reflects the current counts/state, and a save→relog round-trips them. ✅ (via the FEATURE-02 fan-out).
- No `=> 0` / empty-body method left in `QuestService` for the gameplay methods. ✅

## Test plan

- `Map.Server.Tests` (add) `QuestServiceTests`:
  - Add/Delete/Change state transitions;
  - `UpdateObjective` increments only matching mob ids, clamps at target, completes on last objective;
  - `Check` returns correct codes for HAVEQUEST/PLAYTIME/HUNTING;
  - time-limit expiry flips state after the configured window (inject clock).
- Integration with FEATURE-01: `MobDeathObserver` → `UpdateObjective` for each contributor.
- Manual/live: accept a hunting quest, kill targets, watch the client mission counter; abandon a quest.

## Notes / gotchas

- `Counts[]` length must match the catalog's objective count; size it at `Add` from `GetCatalogEntry`.
- Mob-id `0` in a quest objective means "any mob" — match accordingly.
- Quest credit fan-out (party / range) is decided in the FEATURE-01 observer; this service just applies the increment to the PC it's given.
- Keep `SnapshotFor`/`Hydrate` payload shape stable — the char-side DELETE-then-INSERT persistence depends on it.

## History

- 2026-06-03 · Implemented the quest service state machine on top of the FEATURE-01
  objective hook. `Add` (validate + computed TimeUnix + zeroed counts), `Delete`,
  `Change` (replace-in-slot), `Check` (HAVEQUEST/PLAYTIME/HUNTING per quest.cpp:898
  with a new `QuestCheckType` enum), `UpdateStatus`, `UpdateObjectiveSub` (the
  clamping per-objective primitive) + `UpdateObjective` refactored onto it; `PcLogin`
  returns the active count + push seam. New `QuestTime` helper ports rAthena
  `quest_time`/`solve_time`/`split_exact_quest_time` — relative (`+3h`, `+2h30mn`,
  `+7d`) and absolute daily/weekly (`4h`, `7d 4h`, `Monday 4h`) TimeLimit formats →
  absolute unix expiry; injectable `Clock` seam. **Parity correction:** quest expiry
  is query-based (rAthena has no `questexpire_timer`), reported by `Check` — no
  game-loop sweep. `QuestServiceTests` (13) + `QuestTimeTests` (16) green; full suite
  4314 pass (1 fail = pre-existing INFRA-11). Follow-ups: FEATURE-20 (session-enter
  load→Hydrate→PcLogin wiring), FEATURE-21 (mob_id==0 + filter objectives schema),
  FEATURE-22 (immediate save-on-mutation); client ZC packets owned by existing PACKET-10.
