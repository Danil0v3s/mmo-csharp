# COMBAT-28 — ASPD SC + skill contributions (status_calc_aspd / fix_aspd / FREECAST)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-09 (base renewal ASPD formula) · **Blocks:** none

## Problem

COMBAT-09 ported the renewal base ASPD formula (`status_base_amotion_pc`), but it
**zeroes** the SC and skill ASPD contributions: the `(status_calc_aspd(fixed)+val)*agi/200`
term, the `status_calc_aspd(false)` rate term, the FREECAST cast-time speed-up, and
`status_calc_fix_aspd`. So Two-Hand Quicken / Adrenaline Rush / Berserk / ASPD potions
do **not** speed up attacks, and Quagmire / Decrease-AGI-style ASPD debuffs do not slow
them. A buffed assassin attacks at its unbuffed rate.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `RenewalPcAmotion(...)` — computes
  `aspd = (int)temp - min(aspdBase,200)` with the `(fixedSc+val)*agi/200` term **= 0**,
  then the RE %-modifier uses only `aspdRate2` (item `bAspdRate`) with the
  `status_calc_aspd(false)` SC term **= 0**, and no `status_calc_fix_aspd`. Comments mark
  each omission `→ COMBAT-28`.
- `StatusCalcService` does **not** inject `IStatusChangeService`, so it cannot read the
  active SC list. The skill-based `val` terms need `pc_checkskill`/riding state.

## rAthena reference (source of truth)

- `status.cpp:8006` `status_calc_aspd(bl, sc, fixed=true)` — flat ASPD-point bonuses:
  Two-Hand Quicken / One-Hand / Merc Quicken / Adrenaline / Spear Quicken → +7 (gated off by
  Quagmire), Adrenaline2 → +6, Fleet → +5, AssnCros → +val2, Madness Cancel → +20,
  Berserk → +15, ASPD Potion 0-3 → +val1.
- `status.cpp:8056+` `status_calc_aspd(false)` — rate reductions: DontForgetMe (−val2/10),
  EnsembleFatigue, Steel Body (−25), Defender (−val4/10), Gospel-enemy (−75), …
- `status.cpp:2343-2353` skill `val`: SA_ADVANCEDBOOK (Book), SG_DEVIL, GS_SINGLEACTION
  (guns), riding (−50 + cavalier mastery / −25 + dragon training).
- `status.cpp:6156-6172` FREECAST while casting: `amotion = amotion*5*(lv+10)/100`;
  then `+= aspd_add`; then `status_calc_fix_aspd`.

## Scope — every sub-system that must be touched

- [x] Injected `IStatusChangeService` into `StatusCalcService` as a `Lazy<>` (breaks the
      cycle StatusCalcService → SC → DamageService → MobSpawnService → StatusCalcService);
      DI registration uses a factory. Degrades to no SC contribution when null (test ctors).
- [x] Ported `status_calc_aspd(fixed=true)` (`ComputeScAspd`) over the SCs in `StatusType`:
      Quagmire-gated Quicken family (+7), Adrenaline2 (+6), Fleet (+5), AssnCros (+val2),
      Madness (+20), Berserk (+15), ASPD potions (+val1) → the `(fixedSc + val)·agi/200` term.
- [x] Ported `status_calc_aspd(false)` rate term (DontForgetMe −val2/10, Steel Body −25,
      Defender −val4/10, Gospel-enemy −75) into the RE %-modifier alongside `aspd_rate2`.
- [x] Ported `status_calc_fix_aspd` flat-amotion adjustments for the SCs present (Heat Barrel).
- [ ] Skill `val` terms (SA_ADVANCEDBOOK/SG_DEVIL/GS_SINGLEACTION/riding) ➡️ **COMBAT-50**
      (need new skill-id constants + class/mount gates).
- [ ] FREECAST cast-time speed-up ➡️ **COMBAT-50** (cast-state-dependent; SC may need adding).

## Done criteria

- Two-Hand Quicken / Berserk measurably lowers amotion vs the unbuffed value ✅.
- Quagmire raises amotion (gates the quicken off + its AGI cut) ✅.
- A recalc while the buff is active preserves the ASPD bonus ✅ (re-summed from the live SC
  set each `CalcPc` — naturally idempotent, no base/final split).

## Test plan

- Unit-test `RenewalPcAmotion` with a non-zero fixed-SC term and assert the `*agi/200` add.
- Integration: apply SC_TWOHANDQUICKEN, CalcPc, assert amotion < unbuffed; end SC, assert
  it returns.
- ASPD-potion + Berserk + Madness Cancel precedence (max-wins) per `status_calc_aspd`.

## Notes / gotchas

- The SC ASPD term is *naturally* re-fold-safe (it reads the live SC set), unlike the param
  stat re-fold (COMBAT-10) — do NOT block this on COMBAT-10.
- Many of the listed SCs may not yet have `StatusType` members; cover those that exist and
  note (do not stub) the ones that don't — add them when their SC ports.

## History

- **2026-06-02** — inprogress→done. `StatusCalcService` now reads the live SC list (injected
  as `Lazy<IStatusChangeService>` to break the recalc DI cycle) and folds the ASPD
  contributions via a new `ComputeScAspd`: `status_calc_aspd(fixed)` (Quagmire-gated Quicken
  family + Madness/Berserk/AssnCros/ASPD-potions) into the `(fixedSc+val)·agi/200` term,
  `status_calc_aspd(false)` (Steel Body / Defender / Gospel / DontForgetMe) into the
  %-modifier, and `status_calc_fix_aspd` (Heat Barrel) as a flat amotion add. So Two-Hand
  Quicken / Berserk speed up attacks and Quagmire slows them. Combat28AspdScTests (5); unit
  suite 3843 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-50 (skill `val` terms
  + FREECAST + the exotic fix_aspd SCs).
