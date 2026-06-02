# COMBAT-79 — Literal per-accumulator split + DEF-at-end reorder (full battle_calc_weapon_attack fidelity)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
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

- [ ] Restructure `ComputeHandDamage` (or a successor) to carry the five accumulators per hand,
      each element-fixed with its own element source (lhw.ele for status, left/right weapon ele for
      weapon/equip), and `percentAtk = (weaponAtk + equipAtk) * atk_rate / 100`.
- [ ] Move the DEF subtraction (`battle_calc_defense_reduction` equivalent) to **after** the
      skill-ratio + Res steps so patk/mastery/res operate on the pre-DEF value, matching rAthena.
- [ ] Verify every existing combat parity test (COMBAT-18/40/43/56 …) still holds after the
      reorder — this is the risk surface.

## Done criteria

- Dual-wield hand damage matches rAthena byte-for-byte including: `def2 (vit_def) > 0` combined
  with patk, and a character whose status/weapon/equip elements differ.
- No regression in the existing 480 combat tests.

## Test plan

- Numeric tests with `vit_def > 0` + patk (the ordering-sensitive case), and a divergent-element
  fixture (e.g. neutral status + fire weapon endow) per hand.

## Notes / gotchas

- This is the architectural remainder COMBAT-61 explicitly scoped out. Treat the DEF reorder as the
  high-risk change — land it behind the full combat suite, not a partial run.
- Off-hand element *resolution* (populating `LeftWeaponElement` from a `bonus bAtkEle` script) is
  still gated on the unported equip-bonus element parser — same gap as the right hand.
