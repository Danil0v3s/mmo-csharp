# COMBAT-78 — Skill-crit crit_atk_rate ÷200 variant on the skill-damage path

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-61 (normal-attack crit_atk_rate ÷100) · **Blocks:** none
> **Filed by:** COMBAT-61 — it landed the normal-attack (`skill_id == 0`) crit_atk_rate ÷100
> branch on the weapon swing; the `skill_id > 0` ÷200 branch belongs to the skill path.

## Problem

rAthena's `crit_atk_rate` crit bump has two divisors (battle.cpp:7787): a *critical normal
attack* uses `/100`, a *critical skill* uses `/200`. COMBAT-61 implemented the `/100` branch in
`CalcWeaponAttack` (the normal-attack swing, which is always `skill_id == 0`). A skill that
crits and carries `bonus bCritAtkRate` should instead get the `/200` bump — currently the skill
path applies no crit_atk_rate at all (it builds on `swing.Total`, and the swing's `/100` crit
bump is only added when the *swing itself* crit, which is the auto-attack roll, not the skill's).

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcWeaponAttack` — applies `damage += damage * car / 100`
  on `isCritical` (the auto-attack crit). The `➡️ COMBAT-78` comment marks the boundary.
- `Map.Server/Skills/Behaviors/SkillImpl.cs:ComputeSkillDamage` — multiplies `swing.Total` by the
  skill ratio + constant addition; no crit_atk_rate ÷200 application for a critical skill.

## rAthena reference (source of truth)

- `battle.cpp:7787`:
  ```c
  if (wd.type == DMG_CRITICAL || wd.type == DMG_MULTI_HIT_CRITICAL) {
      if (skill_id > 0) { wd.damage += floor(wd.damage * crit_atk_rate / 200); ...damage2... }
      else             { wd.damage += floor(wd.damage * crit_atk_rate / 100); ...damage2... }
  }
  ```
- Switch caveat: monolithic `battle.cpp`, not a split file.

## Scope — every sub-system that must be touched

- [x] Decide where a skill's crit is determined in the C# pipeline and thread `crit_atk_rate` ÷200
      into `ComputeSkillDamage` for a critical skill. → The swing's `IsCritical` is the crit signal;
      `ComputeSkillDamage` applies `raw += raw * car / 200` after the ratio (both hands, since it acts
      on the `swing.Total` accumulator).
- [x] Ensure the normal-attack ÷100 (COMBAT-61) and the skill ÷200 do not double-apply when a
      skill reuses a critical swing. → New skill-aware `IBattleCalculator.CalcWeaponAttack(src,
      target, skillId)` (default interface method → test fakes unaffected): for `skillId > 0` the
      ÷100 bump is SUPPRESSED in the swing, so only the ÷200 in `ComputeSkillDamage` applies. The 3
      ComputeSkillDamage feeders (WeaponSkillImpl.CastendDamageId, SkillAttackService, WeaponSkillResolver)
      build the swing skill-aware only when a plugin owns the ratio (the no-plugin DamageRate fallback
      keeps the basic ÷100 swing). ➡️ The 5 swing-bypass plugins (ArrowShower/MagnumBreak/DoubleStrafe/
      ChainCrushCombo/EarthShaker) compute `swing.Total*ratio/100` directly and still get ÷100 —
      moved to **COMBAT-96**.

## Done criteria

- A critical skill with `bCritAtkRate` 50 adds `damage/4` (÷200×50), not `damage/2`. ✅ (140 → 175)
- A critical normal attack keeps the ÷100 behavior from COMBAT-61. ✅ (140 → 210, regression-guarded)

## Test plan

- Numeric test: critical skill ratio path with crit_atk_rate, asserting the ÷200 bump per hand.

## Notes / gotchas

- The swing already carries the auto-attack crit; verify a skill that does *not* crit clears it so
  the ÷200 path isn't fed a falsely-critical swing.

## History

- 2026-06-03 — Added the skill-aware `IBattleCalculator.CalcWeaponAttack(src, target, skillId)`
  (default interface method delegating to the basic swing, so the many `FixedSwingBattle` test fakes
  are unaffected). For `skillId > 0` the concrete `BattleCalculator` suppresses the auto-attack
  crit_atk_rate ÷100 in the swing; `WeaponSkillImpl.ComputeSkillDamage` then applies the ÷200 skill
  variant (`raw += raw * car / 200`) on `swing.IsCritical` after the ratio (battle.cpp:7787). The 3
  ComputeSkillDamage feeders pass the skill id only when a plugin owns the ratio (the no-plugin
  DamageRate fallback keeps the basic ÷100 swing — no regression). Combat78SkillCritAtkRateTests (4:
  skill ÷200 = 175, no-car = 140, non-crit = 100, normal ÷100 = 210); full suite 4144 pass (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-96 for the 5 swing-bypass plugins still on ÷100.
