# COMBAT-65 — Unbreakable / Intravision consumers + the SC speed table

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Honor the `Unbreakable*` mask in the equip break gate — `SkillSideEffectService.BreakEquip`
      now masks out the wearer's unbreakable slots (`equipMask &= ~UnbreakableMask`) before
      rolling (rAthena `skill_break_equip`, skill.cpp:2840). NOTE: strip uses a *separate*
      `unstripable_equip` mask in rAthena (not the unbreakable mask), so strip is unchanged —
      the unbreakable gate is break-only, which is the faithful semantic.
- [x] Honor `Intravision` in the see-hidden check — `EntityActionGates.CanSee` treats a PC with
      the Intravision equip flag as a detector vs the base hide set (Hiding/Cloaking/…), not
      perfect-hide (rAthena `special_state.intravision`).
- [x] Fold the SC speed table CORE into `StatusCalcService` (`ComputeScSpeed`): the two-phase
      slow/fast max-accumulator + the common movement SCs + Steel Body / Defender overrides +
      the 20..1000 caps; the equip speed delta now folds into the same accumulators (faithful).
      ➡️ The exotic-SC tail + the freecast / hiding-walk early branches moved to COMBAT-84.

## Done criteria

- ➡️ from COMBAT-45: an unbreakable-flagged equip is not broken ✅ (strip is the separate
  unstripable mask — out of scope); an Intravision PC sees a hidden target ✅; SC speed modifiers
  change move speed ✅ (common SCs; the tail ➡️ COMBAT-84).

## Test plan

- Unbreakable mask maps + masks out a break; Intravision pierces base hide (not perfect-hide);
  IncreaseAgi/DecreaseAgi/WindWalk/SlowPotion/SteelBody/Defender speed; equip-only backward-compat.
  ✅ Combat65UnbreakableIntravisionSpeedTests (12).

## History

- 2026-06-03 · Shipped all three consumers. `SkillSideEffectService.BreakEquip` masks out the
  wearer's `Unbreakable*` slots (skill.cpp:2840); `EntityActionGates.CanSee` lets an Intravision
  PC pierce base hide (status.cpp:3756); `StatusCalcService.ComputeScSpeed` ports the
  `status_calc_speed` core (slow/fast two-phase + common SCs + Steel Body/Defender + 20..1000
  caps), with the equip speed delta folded into the accumulators (backward-compatible with
  COMBAT-45). Combat65UnbreakableIntravisionSpeedTests (12); status+combat+skills suite 3368
  green, full suite 4071 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-84 (SC
  speed-table tail: exotic SCs + freecast/hiding-walk early branches).
