# FEATURE-30 — Homunculus per-level growth + exp table

> **Epic:** Gameplay-Companion · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** FEATURE-08 (live homun entity) · **Blocks:** none

## Problem

FEATURE-08 left `HomunculusService.GetMaxHp`/`GetMaxSp` as a placeholder linear curve
(`100 + (lv-1)*50`) and `GainExp` on a naive `lv*1000` curve. rAthena derives the homun's HP/SP/stats
from the `homunculus_db` `Base*` + randomized `GrowthMin`/`GrowthMax` ranges per level, and the level
exp from the `exp_homunculus` table.

## Current state (C#)

- `Map.Server/Homunculus/HomunculusService.cs` `GetMaxHp`/`GetMaxSp` (placeholder), `GainExp`
  (`lv*1000`), `LevelUp`.
- `Core.Database/Entities/HomunculusDbEntity.cs` — confirm/extend with the Base/GrowthMin/GrowthMax
  stat columns; importer mapping.

## rAthena reference

- `rathena/src/map/homunculus.cpp` `hom_levelup` — per-level stat growth from
  `homunculus_db` `Base`/`GrowthMin`/`GrowthMax` (randomized), `status_calc_homunculus`.
- `db/re/exp_homunculus.yml` — the homun exp curve.

## Scope

- [ ] Load the homun growth columns + the `exp_homunculus` table (EF entity + importer + seed).
- [ ] `GetMaxHp`/`GetMaxSp`/stats from the real growth ranges (seeded RNG for the randomized part).
- [ ] `GainExp`/`LevelUp` against the real exp table.

## Done criteria

- Level-up applies HP/SP/stat growth within the `homunculus_db` min/max range; the exp-to-next
  matches `exp_homunculus`.

## Test plan

- `HomunculusServiceTests` — level-up growth lands within the seeded min/max range; exp curve matches.
