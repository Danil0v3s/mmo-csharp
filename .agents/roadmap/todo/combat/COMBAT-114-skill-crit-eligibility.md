# COMBAT-114 — Skill crit-eligibility (most skills don't crit) — skill_db crit flag

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-78 (the skill-aware swing) · **Blocks:** none
> **Filed by:** COMBAT-96 — while routing the bypass plugins through the ÷200 crit_atk_rate, noted
> that the skill swing rolls a critical (and applies the ×1.4 crit damage) for EVERY weapon skill,
> but in rAthena most skills cannot crit.

## Problem

`IBattleCalculator.CalcWeaponAttack(src, target, skillId)` rolls a critical (Cri vs Luk) and applies
the ×1.4 crit damage regardless of skill — so EVERY weapon skill (ComputeSkillDamage path + the
COMBAT-96 bypass plugins: ArrowShower/MagnumBreak/DoubleStrafe/ChainCrushCombo/EarthShaker) can crit.
In rAthena a skill crits only when it is crit-eligible (e.g. `battle_config.crit_in_*`, the skill's
`Flags: CritScaleStr`/`IgnoreFlee` family, or specific arms like RG_BACKSTAP). Most skills never roll
a critical; the swing should be built non-crit for them.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcWeaponAttack` — the crit roll is unconditional (no
  skill-eligibility gate); both the auto-attack and skill paths crit.
- `Map.Server/Skills/Behaviors/SkillImpl.cs:ComputeSkillDamage` + `ApplySkillCritAtkRate` — consume
  whatever `swing.IsCritical` the calculator produced.

## rAthena reference (source of truth)

- `battle.cpp battle_calc_weapon_attack` — the `is_attack_critical` gate (skill_id, skill flags,
  battle_config). Skills set `wd.type = DMG_CRITICAL` only when eligible.

## Scope

- [ ] Add a skill crit-eligibility gate (skill_db crit flag / a curated set, mirror the COMBAT-92
      column pattern) so `CalcWeaponAttack(skillId)` only rolls a crit for crit-eligible skills.
- [ ] Auto-attack (skillId 0) keeps the unconditional roll.

## Done criteria

- A non-crit-eligible skill never produces a critical swing (no ×1.4, no crit_atk_rate); the
  crit-eligible skills still crit. Matches rAthena's `is_attack_critical`.

## Test plan

- A non-eligible skill swing is never IsCritical even at Cri 1000; an eligible one still crits.
