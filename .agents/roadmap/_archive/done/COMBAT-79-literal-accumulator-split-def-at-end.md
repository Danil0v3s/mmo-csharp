# COMBAT-79 — Literal per-accumulator split + DEF-at-end reorder (full battle_calc_weapon_attack fidelity)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-03) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-61 (patk/crit/res terms) · **Blocks:** none
> **Filed by:** COMBAT-61 — it delivered the *observable* trait-stat terms (patk/crit/res) inside
> the existing shared `ComputeHandDamage`, but did not build the literal five-accumulator
> architecture nor move the DEF subtraction to rAthena's post-ratio position. Those remain.

## Problem

rAthena's renewal `battle_calc_weapon_attack` builds **five named per-hand accumulators** —
`statusAtk`/`statusAtk2`, `weaponAtk`/`weaponAtk2`, `equipAtk`/`equipAtk2`, `percentAtk`/
`percentAtk2`, `masteryAtk`/`masteryAtk2` — each **element-fixed independently** (statusAtk2 uses
`lhw.ele`, weaponAtk2/equipAtk2 use the left weapon's `bonus bAtkEle`), with
`percentAtk = (weaponAtk + equipAtk) * atk_rate / 100`. It then assembles `wd.damage` and applies,
**in this order**: patk → +mastery → crit_atk_rate → short/long → skill ratio → constant add →
katar mastery → **Res** → **DEF reduction (battle_calc_defense_reduction, the LAST step)**.

The C# `ComputeHandDamage` is a single shared pipeline that (a) does not separate the five
accumulators (so divergent per-accumulator elements collapse to one weapon-element fix) and
(b) applies the **DEF subtraction early** (before mastery/patk/res) rather than last. COMBAT-61's
patk/crit/res match rAthena exactly for representative values where this ordering is equivalent
(vit_def small/zero, single weapon element), but diverge when `def2 (vit_def) > 0` interacts with
patk, or when status/weapon/equip carry different elements.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — one shared per-hand pipeline:
  base → bAtkRate → size → element (single weapon element) → **DEF subtract (early)** → patk →
  mastery → cardfix → SC bumps → Res → floor. No statusAtk/weaponAtk/equipAtk/percentAtk split.
- `Map.Server/Combat/BattleCalculator.cs:CalcBaseDamage` — the trimmed renewal base-atk slice.

## rAthena reference (source of truth)

- `battle.cpp:4036-4079` — accumulator construction + per-accumulator `battle_attr_fix`
  (`statusAtk2 += batk` then attr-fixed by `lhw.ele`; `weaponAtk2`/`equipAtk2` by `left_element`;
  `percentAtk2 = (weaponAtk2 + equipAtk2) * atk_rate / 100`).
- `battle.cpp:7768-7803` — `wd.damage = statusAtk + weaponAtk + equipAtk + percentAtk`, patk,
  +mastery, crit_atk_rate, short/long.
- `battle.cpp:7806-7845` — skill ratio, constant add, katar mastery, Res.
- `battle.cpp:7862` — `battle_calc_defense_reduction` runs **after** all of the above.
- Switch caveat: monolithic `battle.cpp`, not a split file.

## Scope — every sub-system that must be touched

- [x] Move the DEF subtraction (`battle_calc_defense_reduction` equivalent) to **after** the
      skill-ratio + Res steps so patk/mastery/res operate on the pre-DEF value, matching rAthena. →
      `ComputeHandDamage` now applies the `(4000+eDEF)/(4000+10*eDEF) - sDEF` subtraction as the
      final step (after patk / mastery / cardfix / SC bumps / Res), battle.cpp:7862.
- [x] Verify every existing combat parity test still holds after the reorder. → full combat+skills
      suite (3124) + full suite (4144) green, zero regressions (the existing def>0 tests don't
      exercise a patk/mastery/res term between the old early-DEF and the new late-DEF position).
- [ ] Restructure `ComputeHandDamage` to carry the five accumulators per hand, each element-fixed
      with its own element source, and `percentAtk = (weaponAtk + equipAtk) * atk_rate / 100`. ➡️
      **Moved to COMBAT-97** — the PC five-accumulator element split + the ×2 right-hand statusAtk +
      per-accumulator cardfix; high test-recalibration blast radius (the ×2 shifts every batk>0 PC
      baseline, incl. SkillExerciser).

## Done criteria

- Dual-wield hand damage matches rAthena for `def2 (vit_def) > 0` combined with patk (the DEF-order
  case). ✅ (patk-before-def = 103, res-before-def = 49, pinned by Combat79DefAtEndTests)
- No regression in the existing combat tests. ✅ (4144 pass, 1 pre-existing INFRA-11 fail)
- ➡️ The divergent status/weapon/equip element case is **moved to COMBAT-97** (needs the
  five-accumulator split).

## Test plan

- Numeric tests with `vit_def > 0` + patk (the ordering-sensitive case), and a divergent-element
  fixture (e.g. neutral status + fire weapon endow) per hand.

## Notes / gotchas

- This is the architectural remainder COMBAT-61 explicitly scoped out. Treat the DEF reorder as the
  high-risk change — land it behind the full combat suite, not a partial run.
- Off-hand element *resolution* (populating `LeftWeaponElement` from a `bonus bAtkEle` script) is
  still gated on the unported equip-bonus element parser — same gap as the right hand.

## History

- 2026-06-03 — Landed the DEF-at-end reorder: `ComputeHandDamage` now applies the renewal
  `(4000+eDEF)/(4000+10*eDEF) - sDEF` subtraction as the FINAL physical step (after
  patk/mastery/cardfix/SC bumps/Res), matching rAthena battle.cpp:7862 (was applied early). Full
  combat+skills (3124) and full suite (4144) green with ZERO regressions — the existing def>0 tests
  had no patk/mastery/res term spanning the moved position. Combat79DefAtEndTests (3: def-only curve =
  62, patk-before-def = 103, res-before-def = 49). Decomposed the five-accumulator element split (+ ×2
  right-hand statusAtk + per-accumulator cardfix + percentAtk-on-weapon-only) into **COMBAT-97**
  (Size L, high PC-test recalibration blast radius); the divergent-element done-criterion moved there.
