# GP-QUEST-FILTER-DISPLAY — filtered quest objectives show a descriptive label

> **Epic:** gameplay · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> An any-mob quest objective (kill 10 Fish-type / kill 10 on `<map>`) shows a descriptive label in
> the quest window (e.g. "Fish", the map name) instead of "Poring", matching rAthena's
> `clif_quest_string`.

## Player story / why it matters

FEATURE-21 (GP-QUEST) made any-mob filter objectives (Race/Size/Element/Level/Location/allow-list)
actually count kills. They now also appear in the quest-window emits (`ZC_ADD_QUEST`,
`ZC_ADD_QUEST_MISSION`, `ZC_ALL_QUEST_LIST`). Because these objectives have no specific mob, the emit
falls back to `MOBID_PORING` (1002) with an empty name — so the player sees "Poring" / a blank label
for a "kill 10 Fish-type" objective. The **count** is correct; only the **display string** is wrong.

rAthena renders these via `clif_quest_string(objective)`, which builds a human label from the
objective's filters (race name, size, element, "<n> of <map>"). The mob id stays `MOBID_PORING` on
the wire (the client shows the string, not the mob), so only the name field needs the descriptive
text.

## Current state — per layer

| Layer | Exists? | Where / what's missing |
|---|---|---|
| ZC emit (add) | partial | `Map.Server/Quest/QuestService.cs` `EmitAdd` — empty-aegis → mobId 1002, name "" |
| ZC emit (list) | partial | `QuestService.EmitList` — same fallback |
| Label builder | ☐ | no `clif_quest_string` equivalent |

## rAthena reference

- `rathena/src/map/clif.cpp` `clif_quest_string` — composes the objective label from
  race/size/element/map for `mob_id == 0` objectives; used by `clif_quest_add`,
  `clif_quest_send_list`, and the mission packets.

## Scope — every layer

- [ ] Port `clif_quest_string`: given an objective's filters (race/size/element/min-max level/
      location/allow-list), build the descriptive label rAthena uses.
- [ ] In `EmitAdd` / `EmitList` (and the mission companion), when the objective is any-mob, use that
      label as the name field (mob id stays the Poring fallback, matching rAthena).

## Done criteria

- A "kill N Fish-type" objective shows the Fish/race label (not "Poring") in the quest window
  add + login-snapshot emits.
- Specific-mob objectives are unaffected (still show the mob's display name).

## Test plan

- Service/emit test: any-mob objective with `Race1 = "Fish"` → the emitted name field carries the
  race label, not the Poring fallback.

## Notes

- Filed by GP-QUEST (turn 5, FEATURE-21). Cosmetic only — counting is correct; this is the
  quest-window text. Pairs with the project's standing live-client byte-validation pass.
