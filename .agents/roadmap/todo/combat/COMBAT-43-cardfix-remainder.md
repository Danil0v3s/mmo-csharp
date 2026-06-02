# COMBAT-43 — Cardfix remainder (ignore-def, element-debuff, race2, magic arrays)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-21 (multiplicative cardfix + magic-add-race/critical-add-race)
> **Blocks:** none
> **Filed by:** COMBAT-21 — the cardfix sub-stages that live outside the offensive/defensive
> percent grouping or need new infrastructure.

## Problem

COMBAT-21 converted `battle_calc_cardfix` to the per-category multiplicative grouping
and wired magic-add-race + critical-add-race. The remaining `battle_calc_cardfix`
pieces are unported because they live in other stages or need new infrastructure:

1. **Ignore-def** (`bIgnoreDefRace`/`bIgnoreDefClass`) — a DEF-reduction-stage effect
   (battle.cpp:3379), not cardfix. Needs a constant-value extractor parse
   (`bonus bIgnoreDefRace,RC_DemiHuman;` — the arg is a race CONSTANT, not a number)
   and an apply in `BattleCalculator.ComputeHandDamage`'s defense reduction.
2. **Element-debuff** (`battle_calc_cardfix_debuff`, battle.cpp:667) — folds the target's
   SC_MAGIC_POISON / SC_CLIMAX_BLOOM / SC_CLIMAX_EARTH / SC_MISTY_FROST / SC_CLOUD_POISON
   into the magic damage; needs `IStatusChangeService` injected into `BattleCardService`
   (mind the DI cycle — use Lazy).
3. **race2** (`bAddRace2`/`bSubRace2`) — a separate classification (`status_get_race2`:
   guardian/scaraba/etc.), not the BattleRace enum.
4. **Separate magic arrays** — magic currently reuses the weapon `AddEle`/`AddSize`/
   `AddClass`; rAthena has distinct `magic_addele`/`magic_addsize`/`magic_addclass`.
5. **SubDefEle** (`magic_subdefele`), **magic_subsize**, the flag-matched `subele2`/
   `subrace3` lists, and arrow-specific `arrow_addrace`/`arrow_addele`.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — multiplicative; uses
  `AddRace`/`MagicAddRace`/`AddEle`/`AddSize`/`AddClass` + `Sub*`. No debuff, no race2,
  no SubDefEle, no flag-matched lists; magic shares the weapon ele/size/class arrays.
- `EquipBonusBundle` has `IgnoreDefRace`/`IgnoreDefClass` (bitmask fields, COMBAT-21) but
  nothing reads them yet, and the extractor does not parse the constant-value form.

## rAthena reference (source of truth)

- `battle.cpp:711-1151` full `battle_calc_cardfix`; `:667` debuff; `:3379` ignore-def in
  `battle_calc_attack_def` / the def-reduction path.

## Scope — every sub-system that must be touched

- [ ] Extractor: parse `bonus bIgnoreDefRace,<race>` / `bonus bIgnoreDefClass,<class>`
      (constant-value form) into the `IgnoreDefRace`/`IgnoreDefClass` bitmasks.
- [ ] `ComputeHandDamage` def stage: when the target's race/class bit is set, skip the
      hard+soft DEF subtract (right + left hand independently per rAthena).
- [ ] `BattleCardService`: inject `IStatusChangeService` (Lazy) and fold
      `battle_calc_cardfix_debuff` into the BF_MAGIC branch.
- [ ] Add the distinct magic ele/size/class arrays + race2 (`AddRace2`/`SubRace2` via a
      race2 classifier) + `SubDefEle` + `magic_subsize` + the flag-matched `subele2`/
      `subrace3` lists + arrow-specific `arrow_addrace`/`arrow_addele`.

## Done criteria

- ➡️ from COMBAT-21: ignore-def zeroes the DEF subtract vs the carded race/class;
  element-debuff increases magic damage vs a target carrying the SC.
- race2 cards apply; magic uses its own ele/size/class arrays.

## Test plan

- `Combat43CardfixRemainderTests`: ignore-def (def subtract skipped), element-debuff
  (+50% with SC_MAGIC_POISON), race2 add/sub, magic-array isolation.

## Notes / gotchas

- Ignore-def is a DEF-stage effect, not a cardfix multiplier — keep it in
  `ComputeHandDamage`, not `CalcCardFix`.
