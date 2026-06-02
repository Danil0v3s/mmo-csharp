# COMBAT-61 — Full per-hand renewal weapon-attack accumulator split

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
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

- [ ] Model the per-hand accumulators (`statusAtk2`/`weaponAtk2`/`equipAtk2`/
      `percentAtk2`/`masteryAtk2`) as part of a combined right+left renewal-base-damage
      rewrite (coordinate with `CalcBaseDamage`).
- [ ] Apply the per-hand `patk` %, the left-hand `crit_atk_rate/200` crit bump, and the
      `(5000 + res)/(5000 + 10*res)` RES reduction on the off-hand `damage2`.

## Done criteria

- A dual-wielding character's right/left hand damage matches rAthena's accumulator
  output (incl. patk/crit/res) at representative trait-stat values.

## Test plan

- Per-hand accumulator + patk/crit/res numeric tests against rAthena reference values.

## Notes

- Separate from COMBAT-40 (mastery/element), which is the per-hand input fidelity. This
  is the larger base-damage-pipeline rewrite the COMBAT-40 ticket flagged as optional.
- The off-hand element *resolution* (populating `LeftWeaponElement` from a `bonus
  bAtkEle` script) is still gated on the unported equip-bonus element parser — the same
  gap as the right hand; COMBAT-40 wired the *usage* (ComputeHandDamage applies it when
  set).
