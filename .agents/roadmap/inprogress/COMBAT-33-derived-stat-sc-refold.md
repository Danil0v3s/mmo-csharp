# COMBAT-33 — Re-fold derived-stat SC mods on recalc (Angelus Def2, Provoke Batk%, …)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** L · **Player-visible:** yes
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

- [ ] Introduce a centralized post-`CalcMisc` SC re-application pass: after the
      derived stats are recomputed, re-add the active SC derived-stat
      contributions (the rAthena SCB model). Likely needs CalcPc to see the
      entity's active StatusChange list (inject an accessor, or have the recalc
      caller pass active SCs) — coordinate with the COMBAT-31 DI work.
- [ ] Migrate derived-stat SC handlers (Angelus, Provoke, and the full SCB_BATK /
      SCB_DEF2 / SCB_HIT / SCB_FLEE / SCB_CRI / SCB_MATK set) so their contribution
      is re-applied on recalc rather than only at OnStart.
- [ ] Keep primary-stat SC mods working via COMBAT-10's snapshot (don't double).

## Done criteria

- Angelus active → recalc → Def2 bonus preserved.
- Provoke active → recalc → Batk% / Def% deltas preserved.
- No double-count across repeated recalcs.

## Test plan

- Unit: apply Angelus, recalc, assert Def2 still includes the bonus.
- Unit: apply Provoke, recalc, assert Batk/Def deltas preserved + idempotent.

## Notes / gotchas

- This is the larger half of COMBAT-09's original "SC re-fold ordering" axis. The
  cleanest end-state is to stop SC handlers mutating `BattleStats` directly and
  route every stat-affecting SC through a recalc-flag re-application — a sizeable
  refactor across StatusEffectRegistry.
