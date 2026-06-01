# SC-14 — Aurablade / Gravitation / Parrying combat reads

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none · **Split from:** SC-04

## Problem

Three SC-04 starved SCs need attacker-side / stat-calc / block consumers:

1. **Aurablade** (`SC_AURABLADE`, LK) — adds flat bonus damage per hit when the bearer ATTACKS
   (`Val2 = 20*Val1`). No attacker-side read in the weapon-damage path.
2. **Gravitation** (`SC_GRAVITATION`, HW) — while channeling, the bearer suffers a movement /
   ASPD / attack penalty. No stat-calc read.
3. **Parrying** (`SC_PARRYING`, LK) — grants a chance to fully block incoming melee hits for N
   blocks (`Val2` chance, block count). No block read.

## Current state (C#)

- `Map.Server/Status/StatusEffectRegistry.cs` — Aurablade (~1176) `Val2=20*Val1`; Gravitation
  (~1578); Parrying (~1191). None are read by combat / stat calc.

## rAthena reference (source of truth)

- Aurablade: `battle.cpp battle_calc_weapon_attack` — `ATK_ADD(... val2 ...)` flat per hit.
- Gravitation: `status.cpp status_calc_*` — applies the speed/ASPD/atk penalty while active.
- Parrying: `battle.cpp` melee-hit — roll `val2`% to block, decrement the block count.

## Scope — every sub-system that must be touched

- [ ] Aurablade: add the flat bonus to the bearer's weapon damage in `BattleCalculator`
      (attacker-side SC read) or `DamageService.PerformMeleeAttack`.
- [ ] Gravitation: apply the movement/ASPD/attack penalty in `StatusCalcService.CalcPc` (or the
      ASPD path — coordinate with COMBAT-28's `status_calc_aspd`).
- [ ] Parrying: in the melee-hit reduction path (`DamageService.ApplyScDamageReduction`), roll
      `Val2`% to fully block, decrement the count, end at 0.

## Done criteria

- An Aurablade bearer's hits deal `+Val2` flat damage.
- A Gravitation bearer has the rAthena movement/ASPD/attack penalty applied.
- A Parrying bearer blocks melee hits at `Val2`% for the block count, then the SC ends.

## Test plan

- `AurabladeTests` / `GravitationTests` / `ParryingTests` per the above.

## Notes / gotchas

- Aurablade is attacker-side (not the target-reduction path SC-04 used) — needs the
  BattleCalculator attacker-SC hook.
- Gravitation's ASPD penalty overlaps COMBAT-28 (status_calc_aspd SC contributions).
