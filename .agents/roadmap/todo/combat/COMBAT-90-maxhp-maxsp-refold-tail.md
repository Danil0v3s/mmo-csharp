# COMBAT-90 — MaxHp/MaxSp re-fold tail (remaining ~10 handlers)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-73 (the OnRecalcPool seam + verified batch) · **Blocks:** none
> **Filed by:** COMBAT-73 — it built the MaxHp/MaxSp re-fold pass (`OnRecalcPool` +
> `ReapplyMaxHpSpMods`, wired into CalcPc) and gave it to 7 handlers; the rest remain.

## Problem

COMBAT-73 added the `OnRecalcPool` hook (re-apply a SC's MaxHp/MaxSp contribution after CalcPc
rebuilds the pool) + `ReapplyMaxHpSpMods` (the pass) + the CalcPc integration (+ Hp/Sp re-clamp),
and wired 7 handlers (Epiclesis, GtRevitalize, FirmFaith, Lunarstance, FriggSong, Forceofvanguard,
MercSpup). The remaining MaxHp/MaxSp handlers still lack `OnRecalcPool`, so their pool buff/debuff
is wiped on the next recalc.

## Remaining handlers (by variant)

- **Uniform rate `Val2%` → `Val4` snapshot** (mirror the done batch): Appleidun (MaxHp),
  Service4u (MaxSp).
- **No-snapshot, inverse-revert in OnEnd** (mirror Lunarstance): Leradsdew (MaxHp, `Val3%`),
  Deluge (MaxHp, `Val2%`), Melodyofsink (MaxSp **debuff**, `−Val3%`), EnergyDrinkReserch (MaxSp,
  `Val3%`).
- **Flat** (re-apply the flat val, not a %): PromoteHealthReserch (MaxHp `+Val3`).
- **Element-option pools**: CursedSoilOption, PetrologyOption, UpheavalOption (verify each
  OnStart's MaxHp/MaxSp form).
- **Multi-axis (MaxHp half)**: Berserk, PowerOfGaia, Eqc, SolidSkinOption — add the `OnRecalcPool`
  MaxHp re-fold; their derived axis is COMBAT-89.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — the handlers above have `OnRecalcPool == null`.
- The infra is done: `StatusEffectHandler.OnRecalcPool`, `IStatusChangeService.ReapplyMaxHpSpMods`
  / `StatusChangeService.ReapplyMaxHpSpMods`, and the CalcPc call + Hp/Sp re-clamp (COMBAT-73).

## rAthena reference (source of truth)

- `status.cpp status_calc_maxhp_pc` / `status_calc_maxsp_pc`.

## Scope — every sub-system that must be touched

- [ ] Add `OnRecalcPool` to each remaining handler matching its OnStart form (rate → recompute
      on the rebuilt pool + re-store the snapshot; flat → re-add the flat; debuff → re-subtract).
- [ ] For Melodyofsink (a MaxSp debuff), re-apply the reduction; verify Sp re-clamps.

## Done criteria

- Every MaxHp/MaxSp SC survives an equip/level recalc idempotently, current HP/SP not corrupted.

## Test plan

- Extend `Combat53BespokeRefoldTests`' MaxHp/MaxSp rows per remaining handler.

## Notes / gotchas

- Verify each OnStart individually (rate vs flat, MaxHp vs MaxSp, snapshot vs inverse) — a blind
  bulk edit is unsafe.
