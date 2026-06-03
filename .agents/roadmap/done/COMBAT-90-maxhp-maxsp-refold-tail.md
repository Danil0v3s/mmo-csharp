# COMBAT-90 — MaxHp/MaxSp re-fold tail (remaining ~10 handlers)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Added `OnRecalcPool` to every remaining MaxHp/MaxSp handler, matched to its OnStart form:
      - **% snapshot (recompute on rebuilt pool + re-store Val4):** Appleidun, Service4u (MaxSp),
        CursedSoilOption, PetrologyOption, UpheavalOption.
      - **% inverse-revert (no snapshot; OnEnd recomputes its own inverse):** Leradsdew, Deluge
        (MaxHp), EnergyDrinkReserch (MaxSp).
      - **Flat re-add:** PromoteHealthReserch (MaxHp `+Val3`); Eqc, PowerOfGaia, SolidSkinOption
        (`MaxHp += Val1`, Wave-58 active arms — Def/Def2 derived axis is COMBAT-111).
      - **×3 multi-axis:** Berserk (re-apply `MaxHp*2` snapshot; HP-fill is one-time cast, not
        re-applied — CalcPc re-clamps Hp).
- [x] Melodyofsink (MaxSp debuff `-Val3%`) re-applies the reduction + re-clamps Sp; the INT drop
      survives separately via the COMBAT-10 param-base delta.
- [x] Swept the whole registry — **no** MaxHp/MaxSp-mutating handler is left without `OnRecalcPool`
      (the SolidSkinOption/PowerOfGaia/Eqc first registrations are presence-only; the active
      last-wins arms carry the pool effect and got the hook).

## Done criteria

- ✅ Every MaxHp/MaxSp SC survives an equip/level recalc idempotently, current HP/SP not corrupted
  (Combat53BespokeRefoldTests: +10 MaxHp Theory rows, MaxSp Fact→Theory +2 rows, + Melodyofsink and
  Berserk bespoke facts; 61 pass).

## Test plan

- ✅ Extended `Combat53BespokeRefoldTests`' MaxHp/MaxSp rows per remaining handler (+ bespoke facts
  for the Int-coupled Melodyofsink debuff and the HP-filling Berserk ×3).

## Notes / gotchas

- Verify each OnStart individually (rate vs flat, MaxHp vs MaxSp, snapshot vs inverse) — a blind
  bulk edit is unsafe.
- The derived (non-pool) axes of the multi-axis handlers (Eqc Def2, PowerOfGaia/SolidSkin Def,
  Berserk Def/Flee/Batk/AspdRate) are the COMBAT-89/111 `OnRecalc` axis, NOT this ticket.

## History

- 2026-06-03 — Added `OnRecalcPool` to all 14 remaining MaxHp/MaxSp bespoke handlers (% snapshot,
  % inverse-revert, flat, ×3 Berserk, and the Melodyofsink MaxSp debuff), each mirroring its exact
  OnStart pool form so the buff/debuff re-folds onto the CalcPc-rebuilt pool idempotently without
  corrupting current HP/SP. Registry-wide sweep confirms no pool handler is left uncovered.
  Combat53BespokeRefoldTests +14 cases (61 pass); full Map.Server.Tests 4220 pass (1 fail =
  pre-existing INFRA-11 replay-fixture boot). No follow-ups — Done criteria fully met.
