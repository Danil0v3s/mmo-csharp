# COMBAT-78 — Skill-crit crit_atk_rate ÷200 variant on the skill-damage path

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
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

- [ ] Decide where a skill's crit is determined in the C# pipeline (SkillImpl crit flag vs the
      swing's isCritical) and thread `crit_atk_rate` ÷200 into `ComputeSkillDamage` for a
      critical skill, per hand (damage + damage2).
- [ ] Ensure the normal-attack ÷100 (COMBAT-61) and the skill ÷200 do not double-apply when a
      skill reuses a critical swing.

## Done criteria

- A critical skill with `bCritAtkRate` 50 adds `damage/4` (÷200×50), not `damage/2`.
- A critical normal attack keeps the ÷100 behavior from COMBAT-61.

## Test plan

- Numeric test: critical skill ratio path with crit_atk_rate, asserting the ÷200 bump per hand.

## Notes / gotchas

- The swing already carries the auto-attack crit; verify a skill that does *not* crit clears it so
  the ÷200 path isn't fed a falsely-critical swing.
