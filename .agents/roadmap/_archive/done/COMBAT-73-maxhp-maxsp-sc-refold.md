# COMBAT-73 — MaxHp/MaxSp SC re-fold (post-CalcPc pass)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
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

- [x] Built the re-fold pass: added `StatusEffectHandler.OnRecalcPool` + `ReapplyMaxHpSpMods`
      (on `IStatusChangeService` + `StatusChangeService`), and invoked it in `CalcPc` AFTER the
      MaxHp/MaxSp block, then re-clamped `s.Hp`/`s.Sp` against the (possibly re-folded) `s.MaxHp`/
      `s.MaxSp` — a +MaxHP buff adds headroom without clobbering current Hp; a −MaxHP debuff
      re-caps Hp down. The rate-based delta is **recomputed on the rebuilt pool** (not the stale
      snapshot) and the snapshot re-stored so OnEnd reverts the latest amount.
- [x] Wired a verified batch of 7 handlers across the variants (MaxHp rate, MaxSp rate, no-Val4
      inverse): Epiclesis, GtRevitalize, FirmFaith, Lunarstance, FriggSong, Forceofvanguard,
      MercSpup. The remaining ~10 handlers (Appleidun/Service4u/Leradsdew/Deluge/Melodyofsink/
      PromoteHealthReserch/EnergyDrinkReserch/CursedSoil/Petrology/Upheaval + the Berserk/
      PowerOfGaia/Eqc/SolidSkinOption MaxHp halves) ➡️ COMBAT-90 (each needs its OnStart form
      matched individually).
- [x] Used the dedicated `OnRecalcPool` hook (not `IsRecalcReappliedField`) — the generic
      derived path runs before the MaxHp block, so the pool axis needs the separate later pass.

## Done criteria

- A +MaxHP / +MaxSP SC survives an equip/level recalc, and current HP/SP are not corrupted ✅.
- Idempotent across repeated recalcs ✅ (Combat53BespokeRefoldTests: 6 MaxHp rows + 1 MaxSp
  test, survives + Hp/Sp-not-clobbered + idempotent). The remaining handlers ➡️ COMBAT-90.

## Test plan

- `Combat73MaxHpRefoldTests`: apply a +MaxHP SC, recalc, assert MaxHp preserved + Hp not clobbered;
  recalc again → idempotent.

## Notes / gotchas

- Order matters: the pool re-fold runs AFTER the transcendent ×1.25 / equip flat+rate fold so it
  layers on the final max. Keep the HP/SP clamp that CalcPc already applies.

## History

- 2026-06-03 · Built the MaxHp/MaxSp re-fold infrastructure: `StatusEffectHandler.OnRecalcPool` +
  `ReapplyMaxHpSpMods` (runs after CalcPc's MaxHp/MaxSp block), the CalcPc call + Hp/Sp re-clamp
  against the re-folded max (no clobber). The rate delta is recomputed on the rebuilt pool and the
  snapshot re-stored. Wired 7 handlers (Epiclesis/GtRevitalize/FirmFaith/Lunarstance/FriggSong/
  Forceofvanguard/MercSpup) spanning MaxHp/MaxSp/rate/no-Val4 variants. Extended
  Combat53BespokeRefoldTests with 6 MaxHp rows + a MaxSp test (24 total). Status suite green, full
  suite 4118 pass (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-90 for the remaining
  ~10 pool handlers.
