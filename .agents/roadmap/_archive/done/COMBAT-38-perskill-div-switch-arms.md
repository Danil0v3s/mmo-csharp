# COMBAT-38 — Per-skill div_ switch arms (multi_attack + weapon_attack)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] **Discovery:** the context-aware hook already exists — `ModifyDamageData(ref
      BattleDamage, src, target, lv)` — and the plugins already set their per-skill div
      in it (Pierce size+1, BowlingBash 2HSword→2, FatalMenace dagger+1, WindCutter,
      BackStab, …). But it was **never invoked** (dead hook). Wired it into the
      `WeaponSkillImpl.CastendDamageId` damage path **and** the `SkillAttackService`
      funnel, so the WeaponSkillImpl arms now render their div.
- [x] Ported (activated) the WeaponSkillImpl div arms: KN_PIERCE (size+1), KN_BOWLING
      BASH (2HSword→2), SC_FATALMENACE (dagger+1), RA_WUGSTRIKE, RagingQuadrupleBlow,
      ThrowSpiritSphere, FrenzyShot. ➡️ The splash (`RK_WINDCUTTER`, AxeStomp, OverSlash)
      + plain-`SkillImpl` (`RG_BACKSTAP`, KiExplosion, PsychicWave) arms + the
      miscflag/SC-gated tiers are on dead/contextless paths → COMBAT-60.
- [x] Negative-div semantic preserved (display = `abs(dmg.Hits)`, HP delta stays the
      ComputeSkillDamage total; the positive-div per-hit multiply ➡️ COMBAT-60).
- [x] The splash/SkillImpl/contextless arms are captured in COMBAT-60 (not silently
      dropped).

## Done criteria

- KN_PIERCE vs Small/Medium/Large target → div 1/2/3 (rAthena `tstatus->size+1`). ✅
- RK_WINDCUTTER with a 2H sword → div 2. ➡️ Moved to COMBAT-60 (splash dead-ratio path).
- RG_BACKSTAP with a dagger → div 2. ➡️ Moved to COMBAT-60 (plain-SkillImpl path).
- Each ported arm matches rAthena's weapon-type/size gate; no `// TODO` in touched
  files. ✅ (for the WeaponSkillImpl arms)

## Test plan

- `Combat38PerSkillDivTests`: parametrized weapon-type/size → expected div for each
  ported skill; negative gates (wrong weapon → unchanged).

## Notes / gotchas

- Pierce's div is positive (per-hit full) → multiply damage; Sonic Blow's is negative
  (already total). The plugin must declare which, not just the count.

## History

- 2026-06-02 · Found the per-skill div logic already written in each plugin's
  `ModifyDamageData` but the hook was dead (never invoked). Wired `ModifyDamageData`
  into `WeaponSkillImpl.CastendDamageId` + the `SkillAttackService` funnel so the
  WeaponSkillImpl arms render their div (display = `abs(dmg.Hits)`): KN_PIERCE size+1,
  KN_BOWLINGBASH 2HSword→2, SC_FATALMENACE dagger+1, RA_WUGSTRIKE, RagingQuadrupleBlow,
  ThrowSpiritSphere, FrenzyShot. Added `hits` to the test recorder. Combat38PerSkillDiv
  Tests (7); full Map.Server.Tests green except the pre-existing INFRA-11 replay gate.
  Filed COMBAT-60 (splash/SkillImpl arms — WindCutter/BackStab/AxeStomp/OverSlash — +
  the miscflag/ctx hook extension + the positive-div per-hit damage multiply).
