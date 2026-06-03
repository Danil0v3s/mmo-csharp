# PACKET-10-achievement-quest-ui — Achievement & Quest UI client→map packet bridge

> **Epic:** Packet bridge · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-achievement / FEATURE-quest (services + Intif RPCs exist) · **Blocks:** none

## Problem

`Map.Server/Achievement/AchievementService.cs` and `Map.Server/Quest/QuestService.cs` are fully
implemented and `IIntifService` has `AchievementSave`/`AchievementRequest` and
`QuestSave`/`QuestRequest`. But **no client→map UI packet is wired and the server never pushes the
list/update windows**. A player's achievement window and quest journal are blank, mission-hunt
counters never update on the client, achievement rewards can't be claimed, and quests can't be
toggled active/inactive.

## Current state (C#)

- No handler exists for achievement or quest UI packets.
- `Map.Server/Achievement/IAchievementService.cs` — `UpdateAchievement(pc, id, completed)`,
  `UpdateObjective(pc, type, index, value)`, `CheckReward(pc, id)`, `GetReward(pc, id)`,
  `GetTitles(pc)`, `Level(pc)`, `Hydrate(pc, entries)`.
- `Map.Server/Quest/IQuestService.cs` — `Add(pc, questId)`, `Delete(pc, questId)`,
  `UpdateObjective(pc, questId, index, delta)`, `UpdateStatus(pc, questId, status)` (active toggle),
  `PcLogin(pc)`, `Hydrate(pc, entries)`.
- `Map.Server/Services/Intif/IIntifService.cs:79-84` — `QuestSave(pc)`, `QuestRequest(charId)`,
  `AchievementSave(pc)`, `AchievementRequest(charId)`.

## rAthena reference (source of truth)

`rathena/src/map/clif.cpp`:

**Quest** (server→client emitters + one client request):
- `clif_quest_send_list` (`clif.cpp:17840`) → `ZC_ALL_QUEST_LIST` — full journal on login.
- `clif_quest_send_mission` (`clif.cpp:17938`) → `ZC_ALL_QUEST_MISSION` — mission-objective list.
- `clif_quest_add` (`clif.cpp:17980`) → `ZC_ADD_QUEST` — a quest was added.
- `clif_quest_delete` (`clif.cpp:18058`) → `ZC_DEL_QUEST` — a quest was removed.
- `clif_quest_update_objective` (`clif.cpp:18072`) → `ZC_UPDATE_MISSION_HUNT` — kill-counter tick.
- `clif_quest_update_status` (`clif.cpp:18123`) → `ZC_ACTIVE_QUEST` — active/inactive toggle echo.
- `clif_parse_questStateAck` (`clif.cpp:18112`) → **the only client request** (`CZ_ACTIVE_QUEST`):
  `<quest_id>.L <active>.B` → `quest_update_status` (`QuestService.UpdateStatus`).

**Achievement** (server→client emitters + one client request):
- `clif_achievement_list_all` (`clif.cpp:21776`) → `ZC_ALL_ACH_LIST` — full list + total points/level.
- `clif_achievement_update` (`clif.cpp:21818`) → `ZC_ACH_UPDATE` — one achievement progressed/completed.
- `clif_parse_AchievementCheckReward` (`clif.cpp:21852`) → **client request** (`CZ_REQ_ACH_REWARD`):
  `<achievement_id>.L` → `achievement_check_reward` (`AchievementService.CheckReward` / `GetReward`).
- `clif_achievement_reward_ack` (`clif.cpp:21866`) → `ZC_REQ_ACH_REWARD_ACK` — reward-claim result.

**Read `clif_packetdb.hpp`** for every numeric id (these are PACKETVER-versioned, esp. the quest
list/mission structs which changed shape across versions).

## Scope — every sub-system that must be touched

- [ ] **In packets** (`Core.Server/Packets/In/CZ/`):
  - [ ] `CZ_ACTIVE_QUEST` (`clif_parse_questStateAck`) — `<quest_id>.L <active>.B`.
  - [ ] `CZ_REQ_ACH_REWARD` (`clif_parse_AchievementCheckReward`) — `<achievement_id>.L`.
- [ ] **Out packets** (`Core.Server/Packets/Out/ZC/`):
  - [ ] `ZC_ALL_QUEST_LIST` (var-len), `ZC_ALL_QUEST_MISSION` (var-len), `ZC_ADD_QUEST` (var-len),
        `ZC_DEL_QUEST` (`<quest_id>.L`), `ZC_UPDATE_MISSION_HUNT` (var-len), `ZC_ACTIVE_QUEST`.
  - [ ] `ZC_ALL_ACH_LIST` (var-len: total + count + entries), `ZC_ACH_UPDATE` (one entry),
        `ZC_REQ_ACH_REWARD_ACK` (`<result>.B <achievement_id>.L`).
- [ ] **PacketHeader.cs** + **appsettings.packets.json** (most ZC are var-len).
- [ ] **Handlers** (`Map.Server/Handlers/`):
  - [ ] `Quest/QuestActiveHandler` (`CZ_ACTIVE_QUEST`) → `IQuestService.UpdateStatus(pc, questId,
        active)`; echo `ZC_ACTIVE_QUEST`.
  - [ ] `Achievement/AchievementRewardHandler` (`CZ_REQ_ACH_REWARD`) → `IAchievementService.CheckReward`
        / `GetReward`; emit `ZC_REQ_ACH_REWARD_ACK` with the result, grant the reward, then
        `ZC_ACH_UPDATE` for the now-rewarded achievement.
- [ ] **Push-on-login + push-on-change wiring** (extend `IClifWireService` or add
      `IQuestClientService` / `IAchievementClientService`):
  - [ ] On spawn / `PcLogin` → after `QuestService.Hydrate` / `AchievementService.Hydrate`, emit
        `ZC_ALL_QUEST_LIST` + `ZC_ALL_QUEST_MISSION` and `ZC_ALL_ACH_LIST`.
  - [ ] `QuestService.Add` → emit `ZC_ADD_QUEST`; `Delete` → `ZC_DEL_QUEST`; `UpdateObjective` →
        `ZC_UPDATE_MISSION_HUNT`; `UpdateStatus` → `ZC_ACTIVE_QUEST`.
  - [ ] `AchievementService.UpdateAchievement` / `UpdateObjective` → emit `ZC_ACH_UPDATE`.
      Match the rAthena `clif_quest_*` / `clif_achievement_*` call sites — these fire from the
      service, not from a client packet.
- [ ] No new char-side RPC — `QuestSave`/`QuestRequest`/`AchievementSave`/`AchievementRequest` exist.

## Done criteria

- On entering the map, the quest journal and achievement window are populated (lists pushed after
  hydrate); mission-hunt counters increment on the client when a tracked mob is killed.
- Toggling a quest active/inactive via `CZ_ACTIVE_QUEST` persists and echoes `ZC_ACTIVE_QUEST`.
- Claiming an achievement reward via `CZ_REQ_ACH_REWARD` validates completion + un-claimed state,
  grants the reward (items/title/points), persists via `AchievementSave`, and returns the correct
  `ZC_REQ_ACH_REWARD_ACK` result (success / not-completed / already-claimed).
- Adding/removing a quest pushes `ZC_ADD_QUEST` / `ZC_DEL_QUEST`.
- No stub, no `// TODO`.

## Test plan

- Handler tests pinning: reward claim on an incomplete achievement → not-completed result; on an
  already-rewarded one → already-claimed result; quest toggle persists the active flag.
- Service-emit tests: kill-tick produces `ZC_UPDATE_MISSION_HUNT` with the right count; add/delete
  push the right packet.
- Manual: pick up a hunt quest, kill mobs, watch the counter; complete + claim an achievement reward.

## Notes / gotchas

- Most of this subsystem is **server-push**, not request/response — only `CZ_ACTIVE_QUEST` and
  `CZ_REQ_ACH_REWARD` are client-originated. The bulk of the work is wiring the `ZC_*` emitters to
  the existing service mutation points (Add/Delete/UpdateObjective/UpdateAchievement) + the
  on-login list dump.
- Quest list/mission and achievement-list structs are heavily PACKETVER-versioned; pin the layout
  to the target PACKETVER from `clif_packetdb.hpp` and packets_struct.hpp.
- Reward grant must be idempotent — guard against double-claim (rAthena tracks `rewarded` per
  achievement); the ack result distinguishes not-completed vs already-claimed.
