# COMBAT-97 — PC five-accumulator damage parts (per-accumulator element split + ×2 status + percentAtk)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** COMBAT-79 (DEF-at-end reorder) · **Blocks:** none
> **Filed by:** COMBAT-79 — it landed the DEF-at-end reorder (the def2+patk ordering done-criterion);
> the five-accumulator element-split architecture (the divergent-element done-criterion) remains and
> has a large PC-test recalibration blast radius, so it is its own ticket.

## Problem

rAthena's renewal `battle_calc_damage_parts` (battle.cpp:4025, PC-only — mobs compute damage
directly) builds five per-hand accumulators, each element-fixed with its OWN element source:

- `statusAtk` = `batk`, fixed at **ELE_NEUTRAL** (or `rhw.ele`/`lhw.ele` under SC_SEVENWIND),
  then **doubled** for the right hand (`statusAtk *= 2`).
- `weaponAtk` = `battle_calc_base_weapon_attack` (the roll, size-fixed, ×1.4 on crit), fixed by the
  hand's weapon element; + SC_SUB_WEAPONPROPERTY pseudo-element bonus.
- `equipAtk` = `battle_calc_equip_attack`, fixed by the hand's weapon element.
- `percentAtk` = `(weaponAtk + equipAtk) * atk_rate / 100` (atk_rate touches ONLY weapon+equip,
  NOT status).
- `masteryAtk` (no element).

Each is **cardfixed independently** (battle.cpp:7755-7760) before assembly
`wd.damage = statusAtk + weaponAtk + equipAtk + percentAtk`, then patk → +mastery.

The C# `ComputeHandDamage` collapses this into ONE base value (`batk + roll`, ×1.4 crit) and applies
size / element / atk_rate / cardfix to the WHOLE base. This diverges when:
- the status (neutral) and weapon (e.g. fire-endowed) elements differ vs the target's def element;
- `atk_rate` (bAtkRate) is non-zero (it wrongly scales the status portion too);
- the right-hand `statusAtk` should be `2*batk` (the C# uses `1*batk`).

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — single merged pipeline; DEF is now last
  (COMBAT-79). No statusAtk/weaponAtk/equipAtk/percentAtk split; batk added once (no ×2); atk_rate and
  the weapon-element fix apply to the whole base.
- `Map.Server/Combat/BattleCalculator.cs:CalcBaseDamage` — merges roll + batk.
- `Map.Server/Status/BattleStats.cs` — has `Batk`, `WatkMin/Max` (weapon+equip merged), no separate
  `EquipAtk`. (The equipAtk/weaponAtk split is element-equivalent — both use the hand weapon element —
  so equipAtk may fold into weaponAtk; the status split + ×2 + percentAtk-on-weapon-only are the real
  changes.)

## rAthena reference (source of truth)

- `battle.cpp:4025-4088` `battle_calc_damage_parts` (accumulator build + per-accumulator attr_fix + ×2).
- `battle.cpp:7755-7784` per-accumulator cardfix + assembly + patk + mastery.

## Scope — every sub-system that must be touched

- [ ] Restructure the PC branch of `ComputeHandDamage` to carry statusAtk (neutral/SEVENWIND, ×2 right)
      + weaponAtk (weapon element, size, ×1.4 crit) + percentAtk (`weaponAtk * atk_rate/100`) + mastery,
      assembled then patk → mastery → (existing crit_atk_rate/SC/Res/DEF tail). Keep the mob branch
      direct (no accumulators).
- [ ] Verify the right-hand statusAtk ×2 against rAthena (and whether the C# `Batk` is already the
      effective value) BEFORE shipping — this changes every PC result with `batk > 0`.
- [ ] Per-accumulator cardfix (statusAtk vs weaponAtk vs equipAtk vs masteryAtk) where element-based
      card bonuses differ.
- [ ] Recalibrate every affected PC combat/skill test (incl. SkillExerciser batk=1000 users) to the
      rAthena-exact values.

## Done criteria

- Dual-wield hand damage matches rAthena byte-for-byte for a character whose status (neutral) and
  weapon (endowed) elements differ vs the target's def element, and the right-hand `2*batk` term.

## Test plan

- Divergent-element fixture (neutral status + fire-endow weapon vs a fire-resist target) per hand;
  a `batk > 0` + atk_rate fixture pinning the `2*batk` + `weaponAtk*rate` split.

## Notes / gotchas

- HIGH blast radius — the ×2 status doubling alone shifts every PC damage baseline with batk>0
  (SkillExerciser sets batk=1000). Land behind the full combat+skills suite with each baseline
  recomputed to rAthena, not a partial run.
- Off-hand element resolution (populating LeftWeaponElement from `bonus bAtkEle`) is still gated on the
  unported equip-bonus element parser — same gap as the right hand.
