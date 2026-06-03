# FEATURE-20 — Wire quest (+ achievement) load → Hydrate → PcLogin on session enter

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-03 (quest service Hydrate/PcLogin), FEATURE-04 (achievement) · **Blocks:** none

## Problem

The quest service can hydrate, mutate, snapshot and push a quest log — but **nothing
loads the persisted log when a player enters a map**. `IntifService.QuestRequest`
issues `QuestLoadAsync` fire-and-forget (`_ = _questIpc.QuestLoadAsync(charId)`) and
**discards the response**, so `QuestService.Hydrate` is never called and
`QuestService.PcLogin` has no caller. A returning player's accepted quests + kill
progress are invisible in-session until the next save overwrites them with an empty
log. The same gap exists for achievements (`AchievementRequest` is also fire-and-forget).

## Current state (C#)

- `Map.Server/Services/Intif/IntifService.cs:483 QuestRequest(charId)` — `_ = _questIpc.QuestLoadAsync(charId); return 1;` — response dropped.
- `Map.Server/Quest/QuestService.cs` — `Hydrate(pc, entries)` + `PcLogin(pc)` implemented (FEATURE-03) but uncalled.
- `Map.Server/MapGrpcService.cs:129-138` — the party hydrate model (`_partyService.Hydrate` after the char-side party id is known) is the pattern to mirror.
- `Core.Server/Protos/char_service.proto:100` — `rpc QuestLoad` + `QuestLoadResponse` exist and are real char-side.
- `Map.Server/Services/ICharServerIpcService.Quest.cs:7 QuestLoadAsync` returns `QuestLoadResponse?`.

## rAthena reference (source of truth)

- `rathena/src/map/intif.cpp intif_request_questlog` + `mapif_parse_loadquest` →
  `quest_pc_login(sd)`: on the char→map login load, the quest array is filled then
  `quest_pc_login` pushes `clif_quest_send_list` / `clif_quest_send_mission`.

## Scope

- [ ] At the real player map-enter point (TCP spawn / the post-auth wiring block in
      `MapGrpcService` or wherever the live `PlayerEntity` gets its name/session),
      `await` `QuestLoadAsync(charId)`, map the `QuestLoadResponse` rows to
      `Core.Server.IPC.QuestEntryData`, call `QuestService.Hydrate(pc, …)` then
      `QuestService.PcLogin(pc)`. Mirror the party-hydrate fire-and-forget-after-known
      pattern but feed the response back (don't discard it).
- [ ] Same for achievements: consume `AchievementLoadAsync` → `AchievementService.Hydrate`.
- [ ] Ensure load happens before the first autosave so the snapshot doesn't wipe the row.

## Done criteria

- A character with persisted quests logs in and the quest service's `QuestLog` is
  populated; `PcLogin` returns the correct active count and the push seam fires.
- An autosave immediately after enter round-trips the loaded quests (no wipe).

## Test plan

- Integration: a fake quest IPC returning two rows → after enter, `pc.QuestLog` has
  both, `Check(HAVEQUEST)` is active for each.

## Notes / gotchas

- The map-enter sequence puts the placeholder `PlayerEntity` in `MapGrpcService.EnterMap`
  but the named/session player is finalized on the TCP side — hydrate where the live
  entity is fully present, matching how companions/party are loaded.
