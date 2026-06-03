# SCRIPT-04 — Quest & achievement builtins (setquest / completequest / … / achievementadd / …)

> **Epic:** Scripting parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-03 (quest log persistence), FEATURE-04 (achievement subsystem) · **Blocks:** SCRIPT-10 (quest NPCs)

## Problem

Quest- and achievement-driving NPCs are dead. `ctx.player.quest.add(...)` /
`.complete(...)` / `.erase(...)` / `.check(...)` and the achievement equivalents are
all `ScriptStub.CallAsync(...)` no-ops. A player can talk to a quest-giver, get the
"quest accepted" mes line, and have **no quest in their log** — and quest-gated branches
(`if (questprogress(id) == 2)`) always evaluate against `0`, so completion checks never
pass. Achievements never unlock and never push the reward/title.

## Current state (C#)

- `Map.Server/Scripting/Dialog/PlayerSubSurfaces.cs:9-23` — `PlayerQuestSurface`: `add`,
  `complete`, `erase`, `change(from,to)`, `check(id,mode)`, `isBegin`, `showEvent`,
  `refreshInfo`, `showInfo` — all stubs.
- `PlayerSubSurfaces.cs:26-36` — `PlayerAchievementSurface`: `add`, `remove`, `complete`,
  `exists`, `info(id,type)`, `update(id,type,value)` — all stubs.
- `Map.Server/Quest/QuestService.cs` + `IQuestService.cs` + `QuestEntry.cs` — the quest
  subsystem exists (this ticket delegates here). **Verify it persists** (FEATURE-03);
  if quest state is still in-memory, that dependency must land first.
- `Map.Server/Achievement/` — achievement service dir exists; confirm the repo/DB path
  (FEATURE-04) is real before delegating.
- Client packets: quest log uses `ZC_ADD_QUEST` / `ZC_DEL_QUEST` / `ZC_UPDATE_MISSION_HUNT` /
  `ZC_QUEST_NOTIFY_EFFECT` (the last one exists in `Core.Server/Packets/Out/ZC/`). Achievement
  uses `ZC_ACH_UPDATE` / `ZC_ACH_LIST` / reward packets — verify which exist.

## rAthena reference (source of truth)

`script.cpp` + `quest.cpp` + `achievement.cpp`.

- `script.cpp:20957 BUILDIN(setquest)` → `quest_add(sd, quest_id)` (`quest.cpp`): inserts a
  quest at state `Q_ACTIVE`, sets the time limit + hunt targets from `quest_db`, sends
  `ZC_ADD_QUEST` and fires the quest-effect. Optional 2nd arg = the `OnQuestComplete`-style
  trigger NPC.
- `script.cpp:20999 BUILDIN(completequest)` → `quest_update_status(sd, id, Q_COMPLETE)`:
  marks complete, sends update, may grant achievement progress.
- `script.cpp:20980 BUILDIN(erasequest)` → `quest_delete(sd, id)`: removes, sends `ZC_DEL_QUEST`.
- `script.cpp:21015 BUILDIN(changequest)` → erase `from` + add `to` atomically.
- `script.cpp:21035 BUILDIN(checkquest)` → `quest_check(sd, id, type)`: returns the quest
  state (`-1` not present, else `HUNTING`/`Q_ACTIVE`/`Q_COMPLETE`); `type` selects
  "have time"/"hunt count" sub-queries. `questprogress` is the script alias players test.
- `script.cpp:20849 BUILDIN(questinfo)` — registers a quest-marker condition on the current
  NPC (the floating "!" / "?" bubble shown when the player meets the condition). Stored on the
  NPC; evaluated on map load. `showEvent`/`showInfo` are the per-player marker pushes.
- `achievement.cpp` — `achievement_add(sd,id)`, `achievement_update_objective`,
  `achievement_check_complete`/grant (`completequest`-like), reward grant (`achievement_get_reward`),
  title set. `BUILDIN(achievementadd/achievementremove/achievementcomplete/achievementexists/
  achievementupdate/achievementinfo)`.

## Scope — every sub-system that must be touched

- [ ] `PlayerQuestSurface`: `add` → `IQuestService.Add(charId, questId)` (load hunt targets
      from quest_db, set time limit) + `ZC_ADD_QUEST`; `complete` → state Q_COMPLETE + update
      packet; `erase` → delete + `ZC_DEL_QUEST`; `change(from,to)` → erase+add; `check(id,mode)`
      → return state int matching `quest_check` (`mode` = "any"/"hunting"/"time"/"complete");
      `isBegin` → state==active; `showEvent`/`showInfo`/`refreshInfo` → quest-marker pushes
      (`ZC_QUEST_NOTIFY_EFFECT` + the marker list refresh).
- [ ] `questinfo` (NPC-side; lives on `ctx.npc`/registration) → store marker condition, evaluate
      on map enter, push the bubble. Add the registration field if absent.
- [ ] `PlayerAchievementSurface`: `add`/`remove`/`complete`/`exists`/`info`/`update` →
      achievement service + `ZC_ACH_UPDATE`/reward packets.
- [ ] Persistence: ensure quest + achievement writes go through the repo/DB (no in-memory cache;
      respects the project's "no in-memory shortcuts for persisted state" rule).

## Done criteria

- `ctx.player.quest.add(7001)` puts quest 7001 in the player's log (visible client-side) and it
  survives relog. `quest.check(7001)` returns the active state; after `quest.complete(7001)` it
  returns complete. `quest.erase(7001)` removes it.
- A quest-gated dialog branch (`if (await ctx.player.quest.check(id) == COMPLETE)`) works.
- `ctx.player.achievement.add(id)` unlocks it; `complete(id)` grants the reward and pushes the title.
- `questinfo` shows the "!" bubble over the NPC only when the player meets the condition.
- **No `ScriptStub.Call` left in `PlayerQuestSurface` / `PlayerAchievementSurface`.**

## Test plan

- `Map.Server.Tests/Scripting/QuestAchievementBuiltinsTests.cs`: invoke each `ctx.player.quest.*`
  / `.achievement.*` via the engine against a fake `IQuestService`/achievement service; assert
  the delegate call + the ZC packet enqueued. Pin `check` return codes per rAthena mode mapping.
- DB round-trip test (in-memory EF or the test DB harness used elsewhere): add quest → reload
  session → quest present.

## Notes / gotchas

- HARD-GATED on FEATURE-03/04: if quest/achievement persistence isn't real yet, this ticket
  inherits an in-memory stub — do NOT ship it half-persisted. Land the dependency first.
- `check` mode strings must map to rAthena's integer `type` enum exactly — quest scripts test
  specific values; a wrong mapping silently breaks every quest branch.
- `setquest`'s hunt-target seeding comes from `quest_db.yml`; confirm that DB is loaded
  (Navi/quest db loader) or the hunt counters will never populate.
