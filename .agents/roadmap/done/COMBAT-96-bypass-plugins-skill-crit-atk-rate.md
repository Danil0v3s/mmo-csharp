# COMBAT-96 — Route the swing-bypass weapon plugins through the ÷200 skill crit_atk_rate

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-78 (the skill-aware CalcWeaponAttack + ComputeSkillDamage ÷200) · **Blocks:** none
> **Filed by:** COMBAT-78 — it put the crit_atk_rate ÷200 on the `ComputeSkillDamage` authority;
> a handful of weapon plugins compute damage from the raw swing instead and still get ÷100.

## Problem

COMBAT-78 made the skill-damage authority (`WeaponSkillImpl.ComputeSkillDamage`) apply the
crit_atk_rate ÷200 skill variant, and built its swing crit_atk_rate-free via
`IBattleCalculator.CalcWeaponAttack(src, target, skillId)`. But a few weapon plugins **bypass**
`ComputeSkillDamage` and compute damage directly as `swing.Total * ratio / 100`, calling the basic
2-arg `CalcWeaponAttack` — so on a critical hit with `bonus bCritAtkRate` they get the auto-attack
**÷100** bump (battle.cpp:7787 says skills use **÷200**).

## Current state (C#)

Plugins computing `swing.Total * ratio/100` from a basic swing (2-arg CalcWeaponAttack):
- `Map.Server/Skills/Behaviors/Acolyte/ChainCrushCombo.cs:27`
- `Map.Server/Skills/Behaviors/Acolyte/EarthShaker.cs:67`
- `Map.Server/Skills/Behaviors/Archer/ArrowShower.cs:20`
- `Map.Server/Skills/Behaviors/Archer/DoubleStrafe.cs:20`
- `Map.Server/Skills/Behaviors/Swordman/MagnumBreak.cs:31`

(`COMBAT-78` left these on the basic swing to avoid a regression — suppressing their ÷100 without
applying ÷200 would have dropped the bump entirely.)

## rAthena reference (source of truth)

- `battle.cpp:7787` — `skill_id > 0` → `wd.damage += floor(wd.damage * crit_atk_rate / 200)`.

## Scope — every sub-system that must be touched

- [x] Took option (b) for all 5 plugins (keeps their splash/per-victim/inline structure): switched
      each to the skill-aware swing `CalcWeaponAttack(src, target, SkillId)` (÷100 crit_atk_rate
      suppressed) and apply the ÷200 bump via a new shared `SkillImpl.ApplySkillCritAtkRate(dmg, src,
      swing)` helper after its ratio. Plugins: `ArrowShower`, `MagnumBreak` (SplashDamage),
      `DoubleStrafe` (per-hit), `ChainCrushCombo` (after the full ratio incl. GtEnergygain),
      `EarthShaker` (the hidden-target branch — the visible branch already used ComputeSkillDamage).
- [x] Refactored `ComputeSkillDamage`'s inline ÷200 block to call the same `ApplySkillCritAtkRate`
      helper (DRY — single source of truth for the skill crit_atk_rate divisor).

## Done criteria

- ✅ A critical Arrow Shower / Magnum Break / Double Strafe / Chain Crush / Earth Shaker with
  `bCritAtkRate` 50 adds `dmg/4` (÷200), not `dmg/2` — verified for all 5 via the relationship
  `withCar == noCar + noCar*car/200` (≠ the ÷100 value).

## Test plan

- ✅ `Combat96BypassCritTests` (5): each plugin's critical swing with car 50 satisfies the ÷200
  relationship and not the ÷100 one (splash via `SplashDamage` return; inline via recorded damage;
  EarthShaker with the target hidden).

## Notes / gotchas

- These plugins also reuse the auto-attack crit DAMAGE (atkMax ×1.4) as the skill base — a separate
  modeling question (most skills don't crit in rAthena). ➡️ This is system-wide (the whole
  skill-swing path, not just these 5) and out of scope here; filed as **COMBAT-114** (skill
  crit-eligibility gate). This ticket is only the ÷200 bump.

## History

- 2026-06-03 — Routed the 5 swing-bypass weapon plugins (ArrowShower, MagnumBreak, DoubleStrafe,
  ChainCrushCombo, EarthShaker's hidden branch) through the ÷200 skill crit_atk_rate: skill-aware
  swing + a new shared `SkillImpl.ApplySkillCritAtkRate` helper (also adopted by ComputeSkillDamage,
  DRY). `Combat96BypassCritTests` (5, relationship-based); full Map.Server.Tests 4253 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-114 for the system-wide skill crit-eligibility
  question (most skills shouldn't roll a crit at all).
