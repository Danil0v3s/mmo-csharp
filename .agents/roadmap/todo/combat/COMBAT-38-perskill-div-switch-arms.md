# COMBAT-38 — Per-skill div_ switch arms (multi_attack + weapon_attack)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-17 (CalcMultiAttack + GetMultiHitCount), COMBAT-04 (size on stats)
> **Blocks:** none
> **Filed by:** COMBAT-17 — the per-skill_id div_ overrides it did not port.

## Problem

rAthena sets `div_` for a number of skills based on weapon type, target size, or
miscflag — beyond the flat `skill_get_num` the plugin returns. COMBAT-17 wired the
plugin-side `GetMultiHitCount` (default 1, Sonic Blow 8) but did not implement the
context-dependent per-skill div_ arms, so e.g. Pierce vs a Large target still renders
1 hit instead of 3, and 2H-sword Windcutter renders 1 instead of 2.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:WeaponSkillImpl.GetMultiHitCount` — returns
  a constant per plugin (default 1). No weapon-type / size / miscflag input.
- `Map.Server/Combat/BattleCalculator.cs:CalcMultiAttack` — auto-attack only; the
  `switch (skill_id)` arm of `battle_calc_multi_attack` is not ported.

## rAthena reference (source of truth)

- `battle.cpp:4470-4523` `battle_calc_multi_attack` switch: RK_WINDCUTTER (2hsword → 2),
  SC_FATALMENACE (dagger → +1), SR_RIDEINLIGHTNING, RL_QD_SHOT (job_level/20 + C_MARKER),
  KO_JYUMONJIKIRI, MH_BLAZING_AND_FURIOUS (homun spiritball), ABC_FRENZY_SHOT (5*lv% → 3),
  AS_POISONREACT (renewal TF_DOUBLE +1), NW_SPIRAL_SHOOTING / MAGAZINE_FOR_ONE /
  THE_VIGILANTE_AT_NIGHT (weapon-type +N).
- `battle.cpp:7422-7558` `battle_calc_weapon_attack` switch: RG_BACKSTAP (dagger → 2),
  MO_CHAINCOMBO (knuckle → -6), KN_PIERCE/ML_PIERCE (`div_ = ±(size+1)`),
  KN_BOWLINGBASH (2hsword + miscflag → 3/4), MO_FINGEROFFENSIVE, MH_SONIC_CRAW, etc.

## Scope — every sub-system that must be touched

- [ ] Give the weapon-skill plugins a context-aware hit-count hook (extend
      `GetMultiHitCount` to accept src/target, or a new `ResolveDiv(src,target,lv,miscflag)`).
- [ ] Port the per-skill div_ arms for the skills that have C# plugins today (Pierce,
      Backstab, Windcutter, Bowling Bash, Finger Offensive, Fatal Menace, …); each sets
      its plugin div via the new hook with the right weapon-type/size/miscflag math.
- [ ] Preserve the negative-div semantic (single damage shown as N) vs positive-div
      (per-hit ×N) per skill — Pierce is positive (each hit full), Sonic Blow negative.
- [ ] Skip skills with no C# plugin yet; list them in a `log` so the gap is visible.

## Done criteria

- KN_PIERCE vs Small/Medium/Large target → div 1/2/3 (rAthena `tstatus->size+1`).
- RK_WINDCUTTER with a 2H sword → div 2.
- RG_BACKSTAP with a dagger → div 2.
- Each ported arm matches rAthena's weapon-type/size gate; no `// TODO` in touched files.

## Test plan

- `Combat38PerSkillDivTests`: parametrized weapon-type/size → expected div for each
  ported skill; negative gates (wrong weapon → unchanged).

## Notes / gotchas

- Pierce's div is positive (per-hit full) → multiply damage; Sonic Blow's is negative
  (already total). The plugin must declare which, not just the count.
