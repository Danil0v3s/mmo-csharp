# COMBAT-65 — Unbreakable / Intravision consumers + the SC speed table

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-45
> **Blocks:** none
> **Filed by:** COMBAT-45 — the two flag-form consumers + the SC speed table it deferred.

## Problem

COMBAT-45 wired the single-value pc_bonus consumers (speed/crit/usesp/maxweight/
healpower2). Two flag-form consumers + the SC speed table remain:

1. **`bUnbreakable*` (Unbreakable mask)** — the equip break/strip gate must skip a
   break/strip of an equip whose slot is in the unbreakable mask (rAthena `pc_breakequip`
   / `pc_unequipitem` honor `sd->bonus.unbreakable_equip`).
2. **`bIntravision`** — see-hidden: an Intravision PC reveals Hide/Cloak targets in the
   visibility service (rAthena `status_check_visibility` / `OPTION_*` intravision).
3. **SC speed table** — COMBAT-45 folded `bSpeedRate`/`bSpeedAddRate` into
   `StatusCalcService`'s PC speed, but the SC-driven speed modifiers (Increase Agi,
   Quagmire, Decrease Agi, Wind Walk, …, rAthena `status_calc_speed`'s SC table) are not
   folded.

## Current state (C#)

- `EquipBonusBundle` — `Unbreakable*` mask + `Intravision` flag parsed (COMBAT-23), no
  consumer.
- The equip break path (`ISideEffectService.BreakEquip` / `EquipService`) does not check
  the unbreakable mask.
- `Map.Server/Visibility/` — no Intravision see-hidden gate.
- `Map.Server/Status/StatusCalcService.cs` — PC speed folds only the equip speed_rate
  (COMBAT-45), not the SC speed table.

## rAthena reference

- `pc.cpp pc_breakequip` / `pc_unequipitem` (unbreakable mask);
  `status.cpp status_check_visibility` (intravision); `status_calc_speed` (SC table).

## Scope

- [ ] Honor the `Unbreakable*` mask in the equip break/strip gate.
- [ ] Honor `Intravision` in the visibility see-hidden check.
- [ ] Fold the SC speed table into `StatusCalcService`'s PC speed.

## Done criteria

- ➡️ from COMBAT-45: an unbreakable-flagged equip is not broken/stripped; an Intravision
  PC sees a hidden target; SC speed modifiers change move speed.

## Test plan

- Unbreakable blocks a break; Intravision reveals a hidden target; an Agi-Up SC speeds up.
