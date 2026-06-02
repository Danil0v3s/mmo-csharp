# COMBAT-55 — Ranger trap damage (RE_LVL_TMDMOD) via trap-unit handlers

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Add trap-damage unit handlers for the three traps that compute the rAthena base
      (`skill_lv*dex + int*5`) and apply `RE_LVL_TMDMOD` above level 99, plus the
      RA_RESEARCHTRAP multiplier.
- [ ] Wire the trigger (entity steps on / unit tick) → damage via `ctx.Damage`.

## Done criteria

- ➡️ from COMBAT-35: Ranger traps deal the rAthena base damage and use the TMDMOD
  level-scaling formula at level 150.

## Test plan

- Trap base-damage + TMDMOD at lv150 (unit handler unit test).
