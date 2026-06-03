# GP-QUEST-FILTER-INSTANCE — quest Location filter honours instance source map

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes (instance quests only)
> **Depends on:** GP-INSTANCE · **Unlocks:** none

## The deliverable

> While inside an instance, a kill counts toward an any-mob quest objective whose `Location`
> is the instance's **source** map — matching rAthena's `instance_src_map` branch — surviving logout.

## Player story / why it matters

FEATURE-21 (GP-QUEST) implemented the any-mob objective Location filter as a direct map-name-hash
compare: `(uint)Location.GetHashCode() == pc.MapId`. rAthena's `quest_update_objective`
(quest.cpp ~790) has a third branch: if the player is on an **instance** map, the kill still counts
when that instance's `instance_src_map` equals the objective's `mapid`. Players doing kill-quests
inside instanced versions of a map currently get no credit for the Location-gated objective.

This was split out of GP-QUEST because the instance subsystem (GP-INSTANCE) is not built yet — there
is no instance→source-map resolution to call, so the branch is currently unreachable regardless.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| Service logic | partial | `Map.Server/Quest/QuestService.cs` `ObjectiveMatches` — Location check is a plain map-hash compare; no instance branch |
| Instance resolution | ☐ | needs GP-INSTANCE: a way to get the source map for the player's current (instance) map |

## rAthena reference

- `rathena/src/map/quest.cpp` `quest_update_objective` — the `mapid` check:
  `mapid < 0` (any) → pass; `mapid == sd->bl.m` → pass; else if `mapdata->instance_id &&
  mapdata->instance_src_map == mapid` → pass.

## Dependencies — and how to satisfy

- GP-INSTANCE must expose the live map's `instance_src_map` (source map id/name) for the player's
  current map. Then extend `ObjectiveMatches`' Location branch: if the direct compare fails and the
  player is on an instance, compare the objective's Location against the instance source map.

## Scope — every layer

- [ ] Resolve the player's current-map instance source (via the GP-INSTANCE registry).
- [ ] In `QuestService.ObjectiveMatches`, when `Location` doesn't match `pc.MapId` directly, pass
      iff the player's map is an instance whose source map equals `Location`.

## Done criteria

- A player inside an instance of `<map>` killing a mob credits an any-mob objective with
  `Location: <map>`; a player on an unrelated map does not.

## Test plan

- Service test: objective with `Location` set; player on an instance map whose source == Location →
  counts; player on a different map → does not.

## Notes

- Filed by GP-QUEST (turn 5, FEATURE-21). Until then the non-instance Location compare is correct for
  all overworld quests.
