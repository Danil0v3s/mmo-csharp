# COMBAT-28 — ASPD SC + skill contributions (status_calc_aspd / fix_aspd / FREECAST)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
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

- [ ] Inject `IStatusChangeService?` into `StatusCalcService` (optional ctor arg; degrade to
      no-op when null so test ctors keep working). Resolve any DI cycle (lazy or optional).
- [ ] Port `status_calc_aspd(fixed=true)` over the SCs present in the `StatusType` enum and
      feed it into the `(fixedSc + val) * agi / 200` term. Honor the Quagmire gate.
- [ ] Port `status_calc_aspd(false)` rate term into the RE %-modifier alongside `aspd_rate2`.
- [ ] Port `status_calc_fix_aspd` flat SC amotion adjustments (post-conversion).
- [ ] Skill `val` terms (SA_ADVANCEDBOOK/SG_DEVIL/GS_SINGLEACTION/riding) via `pc_checkskill`
      analogue + mount state.
- [ ] FREECAST cast-time ASPD speed-up while the caster has an active cast.

## Done criteria

- Two-Hand Quicken (or Adrenaline / Berserk / an ASPD potion) measurably lowers amotion vs
  the unbuffed value at the same stats.
- Quagmire / Decrease-AGI-class ASPD debuff raises amotion.
- A recalc while the buff is active preserves the ASPD bonus (it is re-summed from the SC
  list each `CalcPc`, so this is naturally idempotent — no base/final split needed).

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
