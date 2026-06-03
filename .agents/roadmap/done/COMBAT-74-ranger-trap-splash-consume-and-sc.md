# COMBAT-74 — Ranger trap detonation: splash AoE + consume + on-hit SC

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-55 (the trap-damage unit handlers) · **Blocks:** none
> **Filed by:** COMBAT-55 — the trap trigger-model refinements beyond the damage formula.

## Problem

COMBAT-55 added the Ranger trap damage units (RA_CLUSTERBOMB / RA_FIRINGTRAP /
RA_ICEBOUNDTRAP) with the exact rAthena damage formula (base + RE_LVL_TMDMOD + Research-Trap
multiplier), detonating on an enemy stepping onto a single trap cell. Three trigger-model
details are simplified vs rAthena and should be made faithful:

1. **Splash AoE on detonation.** rAthena traps have `Splash: true, Range: 3` — detonation hits a
   7×7 area, not just the entity on the trap cell. The COMBAT-55 handler uses `Radius 0`
   (single-cell trigger), so only the stepper is hit.
2. **Consume-on-trigger.** rAthena removes the trap unit when it detonates (`skill_delunit`); the
   COMBAT-55 handler persists the trap for its full `Duration1` (15 s) so successive enemies are
   caught. Confirm per-trap consume semantics and remove the unit on detonation where rAthena does.
3. **On-detonation status effects.** RA_FIRINGTRAP applies `SC_BURNING` and RA_ICEBOUNDTRAP applies
   `SC_FREEZING` (the plugins' `ApplyAdditionalEffects`) — but those fire on the direct skill-hit
   path, not the trap-unit detonation. The unit's `OnPlace` should also apply the trap's on-hit SC.

## Current state (C#)

- `Map.Server/Skills/Units/Handlers/RangerTrapUnits.cs` — `Radius 0`, `DurationMs 15000`,
  `OnPlace` applies `TrapDamage.Compute` to the single stepper; no splash, no consume, no SC.
- `Map.Server/Skills/Behaviors/Archer/FiringTrap.cs` / `IceboundTrap.cs` — `ApplyAdditionalEffects`
  applies the SC on the direct path only.

## rAthena reference (source of truth)

- `db/re/skill_db.yml` RA_* traps: `Splash: true, Range: 3, Duration1: 15000`.
- `skill.cpp` trap `skill_unit_onplace_timer` — detonate (splash via `map_foreachinrange`) +
  `skill_additional_effect` (SC) + `skill_delunit`.

## Scope — every sub-system that must be touched

- [x] Splash the detonation damage to `Range 3` (use the splash-iteration helper) instead of the
      single stepper. → `RangerTrapUnit.OnPlace` now calls `IMapForeachInRangeService.ForEachEnemyInSplash`
      (BCT_Enemy, range 3) over each victim; single-stepper fallback retained for the parameterless ctor.
- [x] Consume the trap unit on detonation where rAthena does (`DelUnitGroup`/`DelUnit`). → consumes via
      injected `Lazy<ISkillUnitService>.DelUnitGroup(group)`; a `group.Units[0].Removed` guard blocks a
      second stepper re-detonating the same tick.
- [x] Apply the trap's on-hit SC (Burning / Freezing / etc.) from the unit `OnPlace`. → `ApplyTrapSc`
      virtual: FiringTrap→SC_BURNING, IceboundTrap→SC_FREEZING (exact `Start(...)` args mirror the
      direct-path `FiringTrap`/`IceboundTrap` behaviors); ClusterBomb→none.

## Done criteria

- A trap detonation damages all enemies within Range 3 and applies the trap's SC; the trap is
  consumed per rAthena semantics.

## Test plan

- `Combat74TrapSplashTests`: detonation hits multiple enemies in range + applies the SC + consumes.

## Notes / gotchas

- COMBAT-55 already supplies the exact damage number via `TrapDamage.Compute` — reuse it for each
  splash victim. Keep the NK_IGNORE* raw-damage application.

## History

- 2026-06-03 — Rewrote `RangerTrapUnit.OnPlace` to splash detonation over Range 3 via
  `IMapForeachInRangeService.ForEachEnemyInSplash` (BCT_Enemy), apply the per-trap on-hit SC
  (`ApplyTrapSc` virtual: Burning/Freezing, args mirroring the direct-path behaviors), and consume the
  trap unit via an injected `Lazy<ISkillUnitService>.DelUnitGroup` with a same-tick re-detonation guard.
  Wired the splash + Lazy<units> into the three trap-unit DI registrations in `Program.cs` (factory form);
  the parameterless ctor keeps the COMBAT-55 single-stepper fallback. Added `Combat74TrapSplashTests`
  (3 cases: Firing splash+burning+consume, Icebound freezing, no-re-detonation). Suite green (4121 pass,
  1 pre-existing INFRA-11 `dhxj.log` replay failure unrelated). No follow-ups — all Scope/Done criteria
  met exactly.
