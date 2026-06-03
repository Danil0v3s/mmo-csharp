# COMBAT-54 — Per-arm RE_LVL_DMOD for the splash / plain-SkillImpl 120/150 arms

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-35, SKILL-17 (ctx-aware ratio via the damage funnel)
> **Blocks:** none
> **Filed by:** COMBAT-35 — these arms' `CalculateSkillRatio` is not consumed by the
> damage funnel today, so a `ReLvlDivisor` override on them is a no-op.

## Problem

COMBAT-35 applied RE_LVL_DMOD(120) to the two **WeaponSkillImpl** arms
(LG_PINPOINTATTACK, KO_JYUMONJIKIRI), which route through
`WeaponSkillImpl.ComputeSkillDamage` (the divisor-applying path,
`SkillImpl.cs:239`). The remaining non-100 arms live on bases whose ratio is NOT
fed through that path:

- **`RecursiveDamageSplashSkillImpl`** plugins override only `CalculateSkillRatio`,
  but the base `SplashDamage` returns 0 and the damage funnel
  (`SkillAttackService.WeaponDamage`, line 172-178) uses the skill_db `DamageRate`
  column for non-`WeaponSkillImpl` plugins — so their `CalculateSkillRatio` (and any
  `ReLvlDivisor` on it) is **dead** for damage:
  - `GC_COUNTERSLASH` (120), `NC_COLDSLOWER` (150), `KO_BAKURETSU` (120),
    `SR_RAMPAGEBLASTER` (120 if target SC_EARTHSHAKER else 150).
- **Plain `SkillImpl`** plugins that route through `ISkillAttackService` likewise do
  not get their `CalculateSkillRatio` divisor-scaled:
  - `NC_FLAMELAUNCHER` (150), `SR_KNUCKLEARROW` (150 if `miscflag&4` else 100),
    `EL_ROCK_CRUSHER` (120).

This is the same root cause SKILL-17 ("ctx-aware ratio via funnel") addresses: the
splash / plain plugin ratio must become the damage authority (replacing the
`DamageRate` fallback) before a per-arm divisor can take effect.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs` — `ComputeSkillDamage` + `ReLvlDivisor`
  live on `WeaponSkillImpl` only; `RecursiveDamageSplashSkillImpl.SplashDamage`
  default returns 0.
- `Map.Server/Skills/SkillAttackService.cs:WeaponDamage` — `DamageRate` fallback for
  non-`WeaponSkillImpl` plugins.
- The 7 plugins above override `CalculateSkillRatio` with the correct rAthena ratio
  but no divisor is applied.

## rAthena reference

- `battle.cpp` arms: 5227 GC_COUNTERSLASH(120), 5312 NC_FLAMELAUNCHER/NC_COLDSLOWER
  (150), 5486/5489 SR_RAMPAGEBLASTER(120/150 cond), 5499 SR_KNUCKLEARROW(150 cond),
  5641 EL_ROCK_CRUSHER(120), 5665 KO_BAKURETSU(120).

## Scope

- [ ] Route the splash + plain-SkillImpl plugin `CalculateSkillRatio` through a
      divisor-applying damage path (coordinate with / depend on SKILL-17), then give
      each plugin a `ReLvlDivisor` (or a `ResolveReLvlDivisor(src,target,miscflag)`
      hook for the conditional arms).
- [ ] Conditional arms: RampageBlaster (target `SC_EARTHSHAKER` → 120 else 150),
      KnuckleArrow (`miscflag&4` → 150 else 100; thread miscflag to the ratio).

## Done criteria

- ➡️ from COMBAT-35: each of these 7 arms scales by its rAthena divisor (incl. the 2
  conditional ones) at level 175/300.

## Test plan

- Per-plugin divisor tests (lv99 vs lv240/300 multiplier) + the 2 conditional branches.
