# COMBAT-63 — Cardfix remainder: element-debuff + race2 + distinct magic arrays + SubDefEle

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Inject `IStatusChangeService` (Lazy) into `BattleCardService` and fold
      `battle_calc_cardfix_debuff` into the BF_MAGIC branch.
- [ ] Add race2 (`AddRace2`/`SubRace2`) via a `status_get_race2` classifier + extractor parse.
- [ ] Add the distinct magic `addele`/`addsize`/`addclass` arrays (+ extractor keys).
- [ ] Add `SubDefEle` / `magic_subsize` / the flag-matched `subele2`/`subrace3` lists /
      arrow-specific `arrow_addrace`/`arrow_addele`.

## Done criteria

- ➡️ from COMBAT-43: element-debuff increases magic damage vs a target carrying the SC;
  race2 cards apply; magic uses its own ele/size/class arrays.

## Test plan

- Element-debuff (+% with SC_MAGIC_POISON); race2 add/sub; magic-array isolation; SubDefEle.
