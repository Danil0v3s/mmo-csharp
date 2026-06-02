# COMBAT-33 — Re-fold derived-stat SC mods on recalc (Angelus Def2, Provoke Batk%, …)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-10 (param-base snapshot established) · **Blocks:** none
> **Filed by:** COMBAT-10 on 2026-06-01 (primary-stat SC mods now survive recalc; derived-stat ones still don't).

## Problem

COMBAT-10 made **primary-stat** SC mods (Blessing +STR, AGI-Up +AGI) survive a
`CalcPc` via the param-base delta snapshot. But SC mods that target **derived**
stats — Angelus (+Def2), Provoke (Batk% / Def%), and any SC writing
Hit/Flee/Cri/Matk/Def2/Mdef2/Batk directly — are still **wiped on every recalc**,
because `CalcMisc` zeroes and recomputes those derived fields from the primary
stats each call. So equipping an item / levelling / allocating a stat while
Angelus or Provoke is active silently drops the buff until the SC re-applies.

This is a **pre-existing** behavior (CalcMisc always reset derived stats); COMBAT-10
did not introduce it and explicitly scoped only the primary-stat preservation
(its Done criteria name Blessing / AGI-Up).

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `CalcPc` zeroes Hit/Flee/Cri/Flee2/
  Def2/Mdef2/Batk/Patk/Smatk/Res/Mres/Hplus/Crate, then `CalcMisc` recomputes them
  from the primary stats — no SC contribution re-added.
- `Map.Server/Status/StatusEffectRegistry.cs` — SC handlers mutate derived stats
  imperatively in OnStart/OnEnd (e.g. Angelus `Stats.Def2 += Val2`,
  Provoke proportional Batk/Def deltas). These deltas are lost on the next CalcPc.

## rAthena reference (source of truth)

`status.cpp status_calc_pc_` re-applies ALL active SC contributions every recalc
via the `SCB_*` recalc-flag system (status_calc_str/agi/.../batk/def2/...). The
C# port instead mutates `BattleStats` directly in SC OnStart/OnEnd, which only
survives recalc for fields not reset by CalcMisc (i.e. the primary stats, now via
COMBAT-10's snapshot).

## Scope — every sub-system that must be touched

- [x] Added a centralized post-`CalcMisc` re-application pass: new
      `OnRecalc` callback on `StatusEffectHandler`; `IStatusChangeService.
      ReapplyDerivedStatMods(Entity)` (default no-op + real impl in
      `StatusChangeService`) iterates the entity's active SCs and invokes each
      `OnRecalc`. `StatusCalcService.CalcPc` calls it after the equip fold (reusing
      the COMBAT-28 `Lazy<IStatusChangeService>` — no new DI).
- [x] Migrated the **generator-default** SCB_* stat-mod set generically (the
      `OnRecalc` re-runs `ApplyCalcFlagDelta` with a new `derivedOnly` filter via
      `IsRecalcReappliedField`), plus the explicit **Angelus** (+Def2), **Provoke**
      (Batk%/Def%) and **Concentration** (Batk/Hit/Def) handlers. ➡️ The remaining
      bespoke derived-stat handlers (Truesight/Overthrust/Magicpower/Reflectshield/
      Drumbattle/Berserk/…) + the `MaxHp`/`MaxSp` SC re-fold → COMBAT-53.
- [x] Primary-stat SC mods keep working via COMBAT-10's snapshot — `derivedOnly`
      skips the 12 primary stats (and AspdRate/MaxHp/MaxSp) so there is no double.

## Done criteria

- Angelus active → recalc → Def2 bonus preserved. ✅
- Provoke active → recalc → Batk% / Def% deltas preserved. ✅
- No double-count across repeated recalcs. ✅ (primary-stat no-double tested too)
- The remaining bespoke derived-stat handlers + MaxHp/MaxSp SC mods surviving recalc
  ➡️ Moved to COMBAT-53 (the named criteria — Angelus, Provoke — are fully met here;
  the broader bespoke sweep is the larger refactor the ticket's Notes warned about).

## Test plan

- Unit: apply Angelus, recalc, assert Def2 still includes the bonus.
- Unit: apply Provoke, recalc, assert Batk/Def deltas preserved + idempotent.

## Notes / gotchas

- This is the larger half of COMBAT-09's original "SC re-fold ordering" axis. The
  cleanest end-state is to stop SC handlers mutating `BattleStats` directly and
  route every stat-affecting SC through a recalc-flag re-application — a sizeable
  refactor across StatusEffectRegistry.

## History

- 2026-06-02 · Added the SCB-style re-fold seam: `StatusEffectHandler.OnRecalc`
  callback + `IStatusChangeService.ReapplyDerivedStatMods` (real impl iterates the
  entity's active SCs → each `OnRecalc`), called from `StatusCalcService.CalcPc`
  after the equip fold via the existing `Lazy<IStatusChangeService>`. Generic
  coverage of the generator-default SCB_* stat-mods (new `derivedOnly` filter on
  `ApplyCalcFlagDelta` + `IsRecalcReappliedField`, skipping primary/AspdRate/MaxHp
  to avoid double-count) plus explicit `OnRecalc` for Angelus/Provoke/Concentration.
  Combat33DerivedStatRefoldTests (5: Angelus/Provoke/Concentration preserved +
  idempotent, generated-SCB derived mod, primary-stat no-double); full
  Map.Server.Tests green except the pre-existing INFRA-11 replay gate. Filed
  COMBAT-53 for the bespoke derived-stat handler remainder + the MaxHp/MaxSp re-fold.
