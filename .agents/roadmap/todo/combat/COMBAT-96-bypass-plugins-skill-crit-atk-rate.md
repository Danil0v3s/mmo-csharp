# COMBAT-96 — Route the swing-bypass weapon plugins through the ÷200 skill crit_atk_rate

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
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

- [ ] For each plugin, either (a) route its damage through `ComputeSkillDamage` (preferred — it
      already owns ratio + ÷200 + bSkillAtk), or (b) build the swing with
      `CalcWeaponAttack(src, target, SkillId)` and apply `dmg += dmg * car / 200` on
      `swing.IsCritical` after its ratio. Keep the splash/per-victim structure intact.

## Done criteria

- A critical Arrow Shower / Magnum Break / Double Strafe / Chain Crush / Earth Shaker with
  `bCritAtkRate` 50 adds `dmg/4` (÷200), not `dmg/2`.

## Test plan

- Extend each plugin's test (or add `Combat96BypassCritTests`) with a critical-swing + car case.

## Notes / gotchas

- These plugins also reuse the auto-attack crit DAMAGE (atkMax ×1.4) as the skill base — a separate
  modeling question (most splash skills don't crit in rAthena); this ticket is only the ÷200 bump.
