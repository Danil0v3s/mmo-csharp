# COMBAT-63 — Cardfix remainder: element-debuff + race2 + distinct magic arrays + SubDefEle

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-43 (ignore-def landed)
> **Blocks:** none
> **Filed by:** COMBAT-43 — the cardfix pieces that need new infrastructure beyond ignore-def.

## Problem

COMBAT-43 landed the ignore-def slice (`bIgnoreDefRace`/`bIgnoreDefClass`). The other
`battle_calc_cardfix` remainder pieces each need new infrastructure:

1. **Element-debuff** (`battle_calc_cardfix_debuff`, battle.cpp:667) — folds the target's
   SC_MAGIC_POISON / SC_CLIMAX_BLOOM / SC_CLIMAX_EARTH / SC_MISTY_FROST / SC_CLOUD_POISON
   into magic damage. Needs `IStatusChangeService` injected into `BattleCardService`
   (mind the DI cycle — use `Lazy<>`; and note the BattleCalculator `_sc`-null-in-prod
   issue, COMBAT-59).
2. **race2** (`bAddRace2`/`bSubRace2`) — a separate classification
   (`status_get_race2`: guardian/scaraba/etc.), not the `BattleRace` enum. Needs a race2
   classifier + the `AddRace2`/`SubRace2` arrays + extractor parse.
3. **Distinct magic arrays** — magic currently reuses the weapon `AddEle`/`AddSize`/
   `AddClass`; rAthena has separate `magic_addele`/`magic_addsize`/`magic_addclass`.
4. **`SubDefEle`** (`magic_subdefele`), **`magic_subsize`**, the flag-matched `subele2`/
   `subrace3` lists, and arrow-specific `arrow_addrace`/`arrow_addele`.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — multiplicative; no debuff, no
  race2, no SubDefEle, no flag-matched lists; magic shares the weapon ele/size/class arrays.
- `EquipBonusBundle` — no race2/SubDefEle/distinct-magic arrays yet.

## rAthena reference

- `battle.cpp:711-1151` `battle_calc_cardfix`; `:667` `battle_calc_cardfix_debuff`.

## Scope

- [x] Injected `IStatusChangeService` (Lazy seam, same cycle break as COMBAT-59) into
      `BattleCardService` and folded `battle_calc_cardfix_debuff` into the BF_MAGIC branch
      (SC_MAGIC_POISON +50; SC_CLIMAX_BLOOM/EARTH +100 on fire/earth; SC_MISTYFROST +15 on
      water; SC_CLOUD_POISON +5×val1 on poison).
- [x] Added the distinct magic `addele`/`addsize`/`addclass` arrays (`EquipBonusBundle`
      `MagicAddEle`/`MagicAddSize`/`MagicAddClass` + `bMagicAddEle`/`bMagicAddSize`/
      `bMagicAddClass` extractor keys); the BF_MAGIC branch now reads them instead of the
      weapon arrays (joining the COMBAT-21 `MagicAddRace` split).
- [ ] race2 (`AddRace2`/`SubRace2` + `status_get_race2` classifier + extractor). ➡️ Moved to
      COMBAT-81 — needs a new mob-classification axis + the mob_db `RaceGroups` data source.
- [ ] `SubDefEle` / `magic_subsize` / flag-matched `subele2`/`subrace3` lists / arrow
      `arrow_addrace`/`arrow_addele`. ➡️ Moved to COMBAT-82 — several distinct defensive/
      flag-matched/arrow arrays, each its own bonus surface.

## Done criteria

- ➡️ from COMBAT-43: element-debuff increases magic damage vs a target carrying the SC ✅;
  magic uses its own ele/size/class arrays ✅. race2 cards apply ➡️ COMBAT-81.

## Test plan

- Element-debuff (MagicPoison/Climax/Misty/Cloud, element-gated); magic-array isolation
  (magic vs weapon addele/addsize/addclass). ✅ Combat63CardfixDebuffMagicArraysTests (9).
  race2 add/sub ➡️ COMBAT-81; SubDefEle/flag-lists/arrow ➡️ COMBAT-82.

## History

- 2026-06-03 · Shipped the element-debuff + distinct-magic-array halves. `BattleCardService`
  gained a `Lazy<IStatusChangeService>` seam (COMBAT-59 pattern) feeding `MagicCardfixDebuff`
  (battle.cpp:667) into the BF_MAGIC offensive cardfix; added `MagicAddEle`/`MagicAddSize`/
  `MagicAddClass` to `EquipBonusBundle` + `bMagicAdd{Ele,Size,Class}` extractor keys, and the
  magic branch now reads them. Combat63CardfixDebuffMagicArraysTests (9); combat+inventory
  suite 600 green, full suite 4053 pass (1 fail = pre-existing INFRA-11 replay gate). Filed
  COMBAT-81 (race2 + status_get_race2 classifier) and COMBAT-82 (SubDefEle/magic_subsize/
  flag-matched subele2-subrace3/arrow arrays).
