# COMBAT-99 — Thread the real BF_* damage flag into CalcCardFix (skill/normal + skill range)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-82 (the flag-matched subele2/subrace3 lists) · **Blocks:** none
> **Filed by:** COMBAT-82 — it added the flag-matched defensive resist lists but derives the attack's
> BF_* flag inside `CalcCardFix` (lane + attacker weapon range, skillmask = both) rather than receiving
> the real flag from the damage pipeline.

## Problem

`battle_calc_cardfix`'s flag-matched lists (`subele2`/`subrace3`) gate on the attack's actual BF_*
flag — `BF_WEAPONMASK` (weapon/magic/misc), `BF_RANGEMASK` (short/long), `BF_SKILLMASK`
(skill/normal). COMBAT-82 derives this inside `CalcCardFix`:
- weapon mask from the lane (exact);
- range mask from the ATTACKER's weapon `AttackRange > 2` (exact for auto-attacks; **wrong for a
  skill** whose range type differs from the weapon, e.g. a melee-weapon char casting a long-range
  skill should be BF_LONG);
- skill mask = `BF_SKILL | BF_NORMAL` (both) — so a `bonus3 bSubRace, RC_X, n, BF_SKILL`
  (skill-only) wrongly also reduces a NORMAL attack, and vice-versa.

So flag-matched resists keyed on **skill-vs-normal** or on a **skill's range type** are imprecise.
Resists keyed on weapon-mask or on an auto-attack's range are exact.

## Current state (C#)

- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` — derives `attackFlag = (int)attackType |
  (ss.AttackRange > 2 ? Long : Short) | Skill | Normal`.
- `Map.Server/Combat/BattleCardService.cs:CalcCardFix` signature — `(attackType, src, target, damage,
  leftHand, attackElement)`; no BF_* flag param.

## rAthena reference (source of truth)

- `battle.cpp:823-826` the `((it.flag)&flag)&BF_*MASK` triple match; `flag` is the real `wd.flag`
  set by the damage pipeline (battle_calc_weapon/magic/misc_attack + skill range type).

## Scope — every sub-system that must be touched

- [ ] Add an optional `int battleFlag = 0` param to `IBattleCardService.CalcCardFix`; when non-zero,
      use it directly for the flag-matched lists (and the long/short offensive split).
- [ ] Thread the real flag from the callers that know it: the auto-attack swing (BF_NORMAL +
      melee/ranged from the weapon) and the skill funnels (BF_SKILL + the skill's range type +
      weapon/magic/misc lane). Keep the COMBAT-82 internal derivation as the `battleFlag == 0` default.

## Done criteria

- A `bonus3 bSubRace, RC_X, n, BF_SKILL` reduces only skill damage (not auto-attacks); a long-range
  SKILL from a melee weapon is treated BF_LONG.

## Test plan

- Flag-matched subrace3 with BF_SKILL vs an auto-attack (no reduction) and a skill (reduction).

## Notes / gotchas

- COMBAT-82 already met its done-criteria for auto-attack range + weapon-mask + magic; this is the
  skill/normal + skill-range precision tail.
