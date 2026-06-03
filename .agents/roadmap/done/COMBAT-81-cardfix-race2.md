# COMBAT-81 — Cardfix race2 (bAddRace2/bSubRace2) + status_get_race2 classifier

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

- [x] Add a `race2`/RaceGroups field to the mob entity + a `status_get_race2` accessor. →
      `MobDbEntry.RaceGroups` was ALREADY loaded from the seeded mob_db (the ticket's "not loaded"
      claim was stale); added `MobEntity.Race2` (cached, from `Race2Map.FromRaceGroups`) + the new
      `BattleRace2` enum + `Race2Map` (RaceGroups key / `RC2_*` token → enum, underscore-normalized).
- [x] Add `AddRace2`/`SubRace2`/`MagicAddRace2` arrays to `EquipBonusBundle` + reset.
- [x] Parse `bonus2 bAddRace2/bSubRace2/bMagicAddRace2` in `BonusScriptExtractor` (via `Race2Map.FromToken`).
- [x] Fold the race2 term into `CalcCardFix` — offensive (target race2: weapon own-category multiply,
      magic folded into the race multiply per battle.cpp:795) + defensive (attacker race2 sub).

## Done criteria

- ✅ a `bAddRace2, RC2_X, 20` card adds 20% vs an X-race2 target; `bSubRace2` reduces incoming damage
  from that race2; magic uses MagicAddRace2. (Combat81Race2Tests — 1000→1200 / →800.)
- ➡️ The **melee per-group multiply** (for a mob in 2+ race2 groups) + **pet** race2 are moved to
  **COMBAT-98** (the C# sums across groups — exact for the 0–1-group common case; the melee `∏` vs
  ranged `Σ` split is part of the broader COMBAT-21 melee/ranged weapon-fold simplification).

## Test plan

- race2 add/sub numeric tests (offensive + defensive); a race2 classifier unit test.

## Notes / gotchas

- race2 is independent of the `BattleRace` enum; do not conflate. The classifier maps mob
  class → one or more RaceGroups (a mob can be in several).

## History

- 2026-06-03 — Added the `BattleRace2` axis + `Race2Map` (mob_db `RaceGroups` keys and `RC2_*` script
  tokens resolve to the same enum via underscore/case normalization), a cached `MobEntity.Race2`
  (rAthena `status_get_race2`), `EquipBonusBundle.AddRace2/SubRace2/MagicAddRace2` (+ Reset), the
  `bonus2 bAddRace2/bSubRace2/bMagicAddRace2` parse, and the race2 cardfix folds in `BattleCardService`
  (offensive weapon own-category multiply + magic folded into the race multiply per battle.cpp:795;
  defensive attacker-race2 sub). The mob_db `RaceGroups` data was already loaded (the ticket's
  unloaded-data claim was stale — no migration/importer needed). Combat81Race2Tests (6: classifier,
  FromRaceGroups, AddRace2 + multi-group sum, SubRace2, MagicAddRace2 + no-leak). Full suite 4153 pass
  (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-98 (melee per-group multiply + pet race2).
