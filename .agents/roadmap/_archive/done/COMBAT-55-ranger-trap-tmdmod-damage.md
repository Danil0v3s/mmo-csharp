# COMBAT-55 — Ranger trap damage (RE_LVL_TMDMOD) via trap-unit handlers

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-35
> **Blocks:** none
> **Filed by:** COMBAT-35 — the trap TMDMOD scaling has no damage path to apply to.

## Problem

COMBAT-35 was to apply `RE_LVL_TMDMOD()` (`damage*150/100 + damage*lv/100` above
level 99) to the Ranger trap skills. But RA_CLUSTERBOMB / RA_FIRINGTRAP /
RA_ICEBOUNDTRAP currently only **place a ground unit** — they have **no damage
computation at all**, so there is nothing to scale yet. The trap-damage unit handler
(compute `skill_lv*dex + int*5`, apply on trigger/tick, then TMDMOD) must be built
first.

## Current state (C#)

- `Map.Server/Skills/Behaviors/Archer/ClusterBomb.cs` / `FiringTrap.cs` /
  `IceboundTrap.cs` — `CastendPos2` calls `_units?.Place(...)`; no damage.
- `Map.Server/Skills/Units/Handlers/` — has tick-damage handlers (e.g.
  `MagnusExorcismusUnit.OnTick` computes + applies damage via `ctx.Damage.ApplyDamage`)
  but none for the Ranger traps.
- `Map.Server/Combat/BattleCalculator.cs:CalcMiscAttack` — generic misc path with the
  unconditional `×lv/100`; the traps don't use it (they have their own base formula).

## rAthena reference

- `battle.cpp:9766` `RE_LVL_TMDMOD()` for RA_CLUSTERBOMB / RA_FIRINGTRAP /
  RA_ICEBOUNDTRAP; base damage `skill_lv*dex + int*5`; `config/const.hpp:95-104`.
- RA_RESEARCHTRAP adds an INT-based trap-damage multiplier.

## Scope

- [x] Add trap-damage unit handlers for the three traps that compute the rAthena base
      (`skill_lv*dex + int*5`) and apply `RE_LVL_TMDMOD` above level 99, plus the
      RA_RESEARCHTRAP multiplier. `TrapDamage.Compute` (shared, exact battle.cpp:9762
      formula incl. the player-no-research→0 and non-player ×200 branches) +
      `ClusterBombUnit`/`FiringTrapUnit`/`IceboundTrapUnit` (divisor 50 / 100 / 100),
      registered in DI so `CastendPos2.Place` now resolves a handler.
- [x] Wire the trigger (entity steps on) → damage via `ctx.Damage`: the trap detonates in
      `OnPlace` (one trigger per entity entry) on its cell. ➡️ The Range-3 **splash** AoE,
      consume-on-detonation, and on-hit SC (Burning/Freezing) are trap trigger-model
      refinements beyond the damage formula — **moved to COMBAT-74**.

## Done criteria

- ➡️ from COMBAT-35: Ranger traps deal the rAthena base damage and use the TMDMOD
  level-scaling formula at level 150. ✅ — `TrapDamage.Compute` verified at lv150
  (ClusterBomb 4500, Firing/Icebound 2250, no-research 0, mob ×200 = 9000) and lv99
  (no TMDMOD = 1500).

## Test plan

- Trap base-damage + TMDMOD at lv150 (unit handler unit test).

## History

- 2026-06-02 — Built the Ranger trap-damage units: `TrapDamage.Compute` (base `skill_lv*DEX+INT*5`
  + `RE_LVL_TMDMOD` above lv99 + Research-Trap multiplier `20*lv/50|100`, player-no-research→0,
  non-player ×200) + `ClusterBombUnit`/`FiringTrapUnit`/`IceboundTrapUnit` (`OnPlace` detonation),
  DI-registered. `Combat55RangerTrapTests` (6, green); Skills+Combat suite 2988 green. Filed
  COMBAT-74 (trap splash AoE + consume + on-hit SC — the trigger-model refinements).
