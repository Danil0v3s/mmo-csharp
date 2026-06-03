# FEATURE-21 — Quest "any-mob" + filter-based objectives (race/size/element/level/map)

> **Epic:** Feature unlock · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-03 (quest objective matching) · **Blocks:** none

## Problem

FEATURE-03 matches kill objectives by the mob's **aegis name** against the flattened
`QuestDbEntity.Mob1/Mob2/Mob3` columns. rAthena quest objectives are richer: an
objective can specify `mob_id == 0` ("any mob") combined with **filters** —
`MinLevel`/`MaxLevel`, `Race`, `Size`, `Element`, `MapName`, and a `MobsAllowed`
whitelist (`quest.cpp quest_update_objective`). The current schema cannot represent
these, so quests like "kill 50 monsters of any kind on this map" or "kill 20
Undead-race monsters" never progress.

## Current state (C#)

- `Core.Database/Entities/QuestDbEntity.cs` — only `Mob1/Count1 … Mob3/Count3` (aegis
  name + count). No mob_id=0, no race/size/element/level/map filter columns.
- `Map.Server/Quest/QuestService.cs UpdateMobObjective` — matches by aegis equality only.
- `Tools.RathenaImporter/Converters/QuestDbConverter.cs` — reads only `Mob`+`Count`
  from each `Targets[]` entry; drops the filter fields.

## rAthena reference (source of truth)

- `rathena/src/map/quest.cpp quest_update_objective` (lines ~757-838) — the
  `objective_check` accumulator: exact mob_id OR (mob_id==0 AND all of min/max level,
  race RC_ALL, size SZ_ALL, element ELE_ALL, map, mobs_allowed whitelist match).
- `rathena/src/map/quest.hpp s_quest_objective` — the full objective struct.
- `db/re/quest_db.yml` — `Targets:` entries carry `Id`, `Mob`, `Count`, `MinLevel`,
  `MaxLevel`, `Race`, `Size`, `Element`, `MapName`, `MobsAllowed`.

## Scope

- [ ] Extend the quest objective schema to a real per-objective table (or JSON column)
      carrying mob_id (0=any), count, and the filter set; EF migration + importer
      (`QuestDbConverter`) mapping from `quest_db.yml`.
- [ ] `QuestService.UpdateMobObjective` — match by mob_id (with the FEATURE-03 aegis
      path retained for legacy rows) AND the filter accumulator when mob_id==0, passing
      the killed mob's race/size/element/level/map. The mob-death observer already has
      the `MobEntity` to read these from.
- [ ] Keep the FEATURE-03 increment/clamp/complete behavior.

## Done criteria

- An "any mob" objective with a race filter advances only on the matching race; a
  map-scoped objective advances only on that map; a level-banded objective respects
  the band — matching rAthena's `objective_check` exactly.

## Test plan

- `QuestServiceTests` — filter matrix: race match/no-match, level in/out of band,
  map in/out, mobs_allowed whitelist.

## Notes / gotchas

- Today's flattened `Mob1/2/3` columns can stay as a fast path for name-only
  objectives; the new structure supplements them. Decide migration vs. dual-read.
