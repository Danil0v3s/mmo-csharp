# COMBAT-43 — Cardfix remainder (ignore-def, element-debuff, race2, magic arrays)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Extractor: parse the constant-arg `bonus bIgnoreDefRace,<race>` /
      `bonus bIgnoreDefClass,<class>` form (new `BonusIgnoreDef` regex +
      `ApplyIgnoreDef`, incl. the `RC_All`/`Class_All` "all bits" sentinel) into the
      `IgnoreDefRace`/`IgnoreDefClass` bitmasks.
- [x] `ComputeHandDamage` def stage: when the attacker's `IgnoreDefRace` bit for the
      target's race (or `IgnoreDefClass` for its class — boss via `MD_STATUSIMMUNE`) is
      set, zero `def1`/`vitDef` (skip the hard+soft DEF subtract). Per hand, since
      ComputeHandDamage runs for the right and left weapon independently.
- [x] Element-debuff (`battle_calc_cardfix_debuff`) ➡️ COMBAT-63 (needs
      `IStatusChangeService` injected into `BattleCardService`).
- [x] race2 + distinct magic ele/size/class arrays + `SubDefEle` + `magic_subsize` +
      flag-matched `subele2`/`subrace3` + arrow-specific ➡️ COMBAT-63 (each needs new
      arrays / a race2 classifier).

## Done criteria

- ➡️ from COMBAT-21: ignore-def zeroes the DEF subtract vs the carded race/class. ✅
- element-debuff increases magic damage vs a target carrying the SC. ➡️ COMBAT-63.
- race2 cards apply; magic uses its own ele/size/class arrays. ➡️ COMBAT-63.

## Test plan

- `Combat43CardfixRemainderTests`: ignore-def (def subtract skipped), element-debuff
  (+50% with SC_MAGIC_POISON), race2 add/sub, magic-array isolation.

## Notes / gotchas

- Ignore-def is a DEF-stage effect, not a cardfix multiplier — keep it in
  `ComputeHandDamage`, not `CalcCardFix`.

## History

- 2026-06-02 · Landed the ignore-def slice: a new `BonusIgnoreDef` regex +
  `BonusScriptExtractor.ApplyIgnoreDef` parse the constant-arg
  `bonus bIgnoreDefRace,RC_X` / `bonus bIgnoreDefClass,Class_X` form (incl. RC_All /
  Class_All → all bits) into the `IgnoreDefRace`/`IgnoreDefClass` bitmasks;
  `BattleCalculator.ComputeHandDamage` zeroes `def1`/`vitDef` when the attacker's
  bit matches the target's race (or class via `MD_STATUSIMMUNE` → Boss), per hand.
  Combat43CardfixRemainderTests (6: extractor race/all/class, def-skip vs race,
  no-fire vs other race, def-skip vs boss). Full Map.Server.Tests green except the
  pre-existing INFRA-11 replay gate. Filed COMBAT-63 (element-debuff + race2 +
  distinct magic arrays + SubDefEle — each needs new infra).
