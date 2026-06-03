# COMBAT-81 — Cardfix race2 (bAddRace2/bSubRace2) + status_get_race2 classifier

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-63 · **Blocks:** none
> **Filed by:** COMBAT-63 — race2 needs a new mob-classification axis + data source, separate
> from the element-debuff + distinct-magic-array work that landed there.

## Problem

rAthena's `battle_calc_cardfix` folds `bAddRace2`/`bSubRace2` (SP_ADDRACE2/SP_SUBRACE2), keyed
on the target's **race2** — a *second* classification (`status_get_race2`: GOBLIN / GUARDIAN /
NINJA / SCARABA / TURTLE / BIOLAB / MANUK / SPLENDIDE / etc., from the mob_db `RaceGroups`),
NOT the `BattleRace` enum. The C# port has no race2 axis, so race2 cards do nothing.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — folds race/ele/size/class +
  magic-debuff (COMBAT-63); no race2 term.
- `Map.Server/Inventory/EquipBonusBundle.cs` — no `AddRace2`/`SubRace2` arrays.
- `Map.Server/Inventory/BonusScriptExtractor.cs` — no `bAddRace2`/`bSubRace2` keys.
- `Map.Server/Entities/MobEntity.cs` / `MobDbEntry` / `Core.Database/Entities/Mob*` — no
  race2 / RaceGroups field, and the mob_db `RaceGroups` column isn't loaded.

## rAthena reference (source of truth)

- `battle.cpp:711-1151` `battle_calc_cardfix` — the `add_race2[...]` / `sub_race2[...]` folds.
- `status.cpp` `status_get_race2` + the mob_db `RaceGroups` YAML block (db/re/mob_db.yml).
- `pc.cpp:4968` SP_MAGIC_ADDRACE2 (magic variant — fold under the magic branch).

## Scope — every sub-system that must be touched

- [ ] Add a `race2`/RaceGroups field to the mob entity + DB entity, load it from mob_db
      (`Tools.RathenaImporter` + the mob_db loader), and a `status_get_race2` accessor.
- [ ] Add `AddRace2`/`SubRace2` (+ `MagicAddRace2`) arrays to `EquipBonusBundle` + reset.
- [ ] Parse `bonus2 bAddRace2/bSubRace2/bMagicAddRace2` in `BonusScriptExtractor`.
- [ ] Fold the race2 term into `CalcCardFix` (offensive + defensive, magic-aware).

## Done criteria

- ➡️ from COMBAT-63: a `bAddRace2, RC2_GUARDIAN, 20` card adds 20% vs a guardian-race2 target;
  `bSubRace2` reduces incoming damage from that race2; magic uses MagicAddRace2.

## Test plan

- race2 add/sub numeric tests (offensive + defensive); a race2 classifier unit test.

## Notes / gotchas

- race2 is independent of the `BattleRace` enum; do not conflate. The classifier maps mob
  class → one or more RaceGroups (a mob can be in several).
