# COMBAT-61 — Full per-hand renewal weapon-attack accumulator split

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-40 (per-hand mastery/element)
> **Blocks:** none
> **Filed by:** COMBAT-40 — its scope item 3 (the full accumulator split) is a separate
> broader rewrite, explicitly out of that ticket.

## Problem

COMBAT-18/40 compute the dual-wield hands through a shared simplified
`ComputeHandDamage` (base damage → bAtkRate → size-fix → element → DEF → mastery →
cardfix → SC bumps → floor), now with per-hand mastery + element (COMBAT-40). rAthena's
renewal `battle_calc_weapon_attack` (battle.cpp:7635) instead builds **separate per-hand
accumulators** — `statusAtk`/`statusAtk2`, `weaponAtk`/`weaponAtk2`, `equipAtk`/
`equipAtk2`, `percentAtk`/`percentAtk2`, `masteryAtk`/`masteryAtk2` — and applies the
trait-stat terms per hand: the `patk` % bump, the `crit_atk_rate/200` left-hand crit
term, and the `(5000 + res)/(5000 + 10*res)` RES reduction on `wd.damage2`. The C# port
does not model these per-hand accumulators or the patk/crit/res left-hand branches.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — one shared pipeline per
  hand; no `statusAtk2`/`weaponAtk2`/`equipAtk2`/`percentAtk2` split, no per-hand
  `patk` / `crit_atk_rate` / `res` terms.
- `Map.Server/Combat/BattleCalculator.cs:CalcBaseDamage` — the trimmed renewal base-atk
  slice this rewrite must coordinate with.

## rAthena reference

- `battle.cpp:7635` body — the per-hand accumulator construction + `patk`/`crit_atk_rate`/
  `res` branches + the `res` reduction on `wd.damage2`.

## Scope

- [x] Apply the per-hand `patk` %, the `crit_atk_rate` crit bump, and the
      `(5000 + res)/(5000 + 10*res)` RES reduction on **both** `damage` and the off-hand
      `damage2`. `patk` (battle.cpp:7775) is applied per hand to the pre-mastery value inside
      `ComputeHandDamage`; `Res` (7845) per hand before the floor; `crit_atk_rate` (7787) on a
      critical in `CalcWeaponAttack` per hand. The normal-attack `/100` divisor is used (this
      function is the `skill_id == 0` swing); the `/200` skill-crit variant ➡️ COMBAT-78.
- [ ] Model the literal per-hand accumulators (`statusAtk2`/`weaponAtk2`/`equipAtk2`/
      `percentAtk2`/`masteryAtk2`) with per-accumulator element fixes + move the DEF subtraction
      to rAthena's post-ratio position. ➡️ Moved to COMBAT-79 (the architectural remainder; the
      observable patk/crit/res output is delivered above, but the literal 5-accumulator split and
      DEF-at-end reorder are a separate higher-risk rewrite).

## Done criteria

- A dual-wielding character's right/left hand damage matches rAthena's accumulator
  output (incl. patk/crit/res) at representative trait-stat values. ✅ patk/crit/res verified
  per hand against hand-computed rAthena references (Combat61PerHandTraitTermsTests). The
  ordering-sensitive residual (`vit_def > 0` × patk, divergent per-accumulator elements)
  ➡️ COMBAT-79.

## Test plan

- Per-hand accumulator + patk/crit/res numeric tests against rAthena reference values.

## Notes

- Separate from COMBAT-40 (mastery/element), which is the per-hand input fidelity. This
  is the larger base-damage-pipeline rewrite the COMBAT-40 ticket flagged as optional.
- The off-hand element *resolution* (populating `LeftWeaponElement` from a `bonus
  bAtkEle` script) is still gated on the unported equip-bonus element parser — the same
  gap as the right hand; COMBAT-40 wired the *usage* (ComputeHandDamage applies it when
  set).

## History

- 2026-06-02 · Landed the three named renewal trait-stat terms per hand in
  `BattleCalculator`: P.ATK % (battle.cpp:7775, pre-mastery), crit_atk_rate ÷100 on a
  critical (7787, normal-attack branch), and the `(5000+res)/(5000+10*res)` Res reduction
  (7845, all sources). Verified per hand vs hand-computed rAthena references in
  `Combat61PerHandTraitTermsTests` (6 tests). Filed COMBAT-77 (res-ignore by race / SC),
  COMBAT-78 (skill-crit ÷200), COMBAT-79 (literal 5-accumulator split + DEF-at-end reorder —
  the architectural remainder of scope item 1).
