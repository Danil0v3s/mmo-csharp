# COMBAT-73 — MaxHp/MaxSp SC re-fold (post-CalcPc pass)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-53 (the OnRecalc seam) · **Blocks:** none
> **Filed by:** COMBAT-53 — the MaxHp/MaxSp axis it did not implement.

## Problem

`StatusCalcService.CalcPc` calls `_sc?.ReapplyDerivedStatMods` (COMBAT-33) at line 200, BEFORE the
MaxHp/MaxSp block (lines ~206-235). `IsRecalcReappliedField` deliberately EXCLUDES MaxHp/MaxSp.
So SC MaxHp/MaxSp mods are still wiped on every recalc — a +MaxHP buff vanishes on equip/level
change. The re-fold must run AFTER the MaxHp/MaxSp computation and preserve the current-HP/SP
clamp semantics (so re-folding MaxHp doesn't clobber current Hp/Sp).

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:CalcPc` — MaxHp/MaxSp computed at ~206-235; no SC re-fold
  after it.
- `Map.Server/Status/StatusEffectRegistry.cs:IsRecalcReappliedField` — returns false for MaxHp/MaxSp.
- The 17 MaxHp/MaxSp bespoke handlers have no OnRecalc:
  Appleidun, CursedSoilOption, Deluge, EnergyDrinkReserch, Epiclesis, FirmFaith, Forceofvanguard,
  FriggSong, GtRevitalize, Leradsdew, Lunarstance, Melodyofsink, MercSpup, PetrologyOption,
  PromoteHealthReserch, Service4u, UpheavalOption (+ Berserk/PowerOfGaia/Eqc/SolidSkinOption's
  MaxHp half, whose derived part is COMBAT-72).

## rAthena reference (source of truth)

- `status.cpp status_calc_maxhp_pc` / `status_calc_maxsp_pc` — the MaxHP/MaxSP rate+flat SC
  adjustments are re-folded each recalc; current HP/SP are then re-capped to the new max.

## Scope — every sub-system that must be touched

- [ ] Add a `ReapplyMaxHpSpMods` pass (a second hook, e.g. `OnRecalcPool`) invoked in `CalcPc`
      AFTER the MaxHp/MaxSp block, re-applying each active SC's MaxHp/MaxSp delta, then re-clamp
      `player.Hp`/`Sp` to the new max (no clobber).
- [ ] Give the 17 MaxHp/MaxSp handlers the pool re-fold hook (snapshot re-apply, like COMBAT-53).
- [ ] (Optionally) add MaxHp/MaxSp to `IsRecalcReappliedField` if the generic delta path is used.

## Done criteria

- A +MaxHP / +MaxSP SC survives an equip/level recalc, and current HP/SP are not corrupted.
- Idempotent across repeated recalcs.

## Test plan

- `Combat73MaxHpRefoldTests`: apply a +MaxHP SC, recalc, assert MaxHp preserved + Hp not clobbered;
  recalc again → idempotent.

## Notes / gotchas

- Order matters: the pool re-fold runs AFTER the transcendent ×1.25 / equip flat+rate fold so it
  layers on the final max. Keep the HP/SP clamp that CalcPc already applies.
