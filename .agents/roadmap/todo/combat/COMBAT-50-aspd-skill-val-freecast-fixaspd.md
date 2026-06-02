# COMBAT-50 — ASPD skill-val terms + FREECAST + exotic fix_aspd SCs

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-28 (the SC ASPD terms + the ComputeScAspd seam)
> **Blocks:** none
> **Filed by:** COMBAT-28 — the skill-based `val` terms + FREECAST it deferred.

## Problem

COMBAT-28 ported the SC ASPD contributions (status_calc_aspd fixed/rate +
status_calc_fix_aspd over the common SCs). Three rAthena pieces remain:

1. **Skill `val` terms** (status.cpp:2343): `SA_ADVANCEDBOOK` (Book weapon → +(lv-1)/2+1),
   `SG_DEVIL` (Star Emperor / max-job → +1+lv), `GS_SINGLEACTION` (guns → +(lv+1)/2),
   and the **riding** penalties (`pc_isriding` → −50 + 10·KN_CAVALIERMASTERY;
   `pc_isridingdragon` → −25 + 5·RK_DRAGONTRAINING). These need skill-id constants not
   yet in `SkillIds` (SA_ADVANCEDBOOK, SG_DEVIL, GS_SINGLEACTION, KN_CAVALIERMASTERY,
   RK_DRAGONTRAINING) + the class/mount gates.
2. **FREECAST** cast-time ASPD speed-up (status.cpp:6156): while casting,
   `amotion = amotion*5*(lv+10)/100`. Cast-state-dependent (and SC_FREECAST may need
   adding to `StatusType`).
3. **Exotic `status_calc_fix_aspd` SCs** (status.cpp:6172): OVERED_BOOST, FIGHTINGSPIRIT,
   SOULSHADOW, SINCERE_FAITH, GUST/BLAST/WILD_STORM options — plus the rate-term positives
   (SwingDance, IncAspdRate, GatlingFever) — that aren't in `StatusType` yet.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs:RenewalPcAmotion` — takes `fixedSc`/`rateSc`/
  `fixAspd`; the skill `val` term is 0 (no skill/mount read).
- `ComputeScAspd` covers Quicken/Madness/Berserk/Potions + Steel Body/Defender/Gospel/
  DontForgetMe + Heat Barrel; the exotic SCs above are absent.

## rAthena reference (source of truth)

- `status.cpp:2343-2353` skill val; `status.cpp:6156-6172` FREECAST + status_calc_fix_aspd.

## Scope — every sub-system that must be touched

- [ ] Add the skill-id constants + thread `pc.LearnedSkills` + `pc.Option` (riding/dragon)
      into a `val` term passed to `RenewalPcAmotion`.
- [ ] FREECAST: while the caster has an active cast, apply the `*5*(lv+10)/100` amotion
      speed-up (and add SC_FREECAST to `StatusType` if missing).
- [ ] Extend `ComputeScAspd` with the exotic fix_aspd + rate SCs as their `StatusType`
      members land.

## Done criteria

- A Gunslinger with GS_SINGLEACTION attacks faster; a riding Knight is slower by the
  cavalry penalty.
- A Free-Cast caster mid-cast has the reduced amotion.

## Test plan

- `Combat50AspdSkillValTests`: GS_SINGLEACTION +val; riding penalty; FREECAST while casting.

## Notes / gotchas

- The SC terms (COMBAT-28) and the skill val are summed together in
  `(status_calc_aspd(fixed)+val)*agi/200` — feed `val` into the existing `fixedSc` add or a
  sibling param.
