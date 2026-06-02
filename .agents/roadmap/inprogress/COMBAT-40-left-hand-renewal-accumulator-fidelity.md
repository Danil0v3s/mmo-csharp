# COMBAT-40 — Left-hand renewal accumulator fidelity (per-hand mastery/element)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-18 (left-hand Damage2 + the AS_RIGHT/AS_LEFT split)
> **Blocks:** none
> **Filed by:** COMBAT-18 — the off-hand damage uses the shared simplified pipeline,
> not the exact renewal dual-accumulator split.

## Problem

COMBAT-18 computes the dual-wield off-hand `Damage2` by running the left weapon
through the SAME simplified `ComputeHandDamage` pipeline as the right hand, then
applies `battle_calc_attack_left_right_hands` (the AS_RIGHT/AS_LEFT/KO split) exactly.
That matches the right hand's fidelity but diverges from rAthena's renewal weapon
attack in two left-hand-specific ways:

1. **Mastery uses the wrong weapon.** `IBattleCardService.AddMastery` is not hand-aware,
   so the left hand's mastery bonus is resolved from the RIGHT weapon type, not the
   off-hand weapon (rAthena `masteryAtk2` uses EQI_HAND_L).
2. **Left weapon element is not distinct.** `EquipBonusAggregator` captures
   `LeftWeaponElement` but always sets Neutral (same placeholder as the right-hand
   `WeaponElement`); rAthena resolves `left_element =
   battle_get_weapon_element(EQI_HAND_L)`.

It also does not model the full renewal accumulator split (`statusAtk2`, `weaponAtk2`,
`equipAtk2`, `percentAtk2`, `masteryAtk2`, the `patk` % and `crit_atk_rate/200` left-hand
crit bump, and the `res` `(5000 + res)/(5000 + 10*res)` reduction) — but that pipeline
is shared with the right hand and is a separate, broader effort.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — shared right/left pipeline;
  calls `_cards.AddMastery(pcAtk, target, damage, Weapon)` (no hand arg) and reads the
  per-hand `weaponElement` (Neutral for the off-hand until the element parser ports).
- `Map.Server/Combat/IBattleCardService.cs:AddMastery` — no `leftHand` parameter.
- `Map.Server/Inventory/EquipBonusAggregator.cs:Aggregate` — `LeftWeaponElement` is
  always `BattleElement.Neutral` (the `bonus bAtkEle` parser is unported, same as rhw).

## rAthena reference (source of truth)

- `battle.cpp:2215` `battle_addmastery` — mastery per weapon (the left hand uses lhw).
- `battle.cpp:7635` body — `weaponAtk2`/`equipAtk2`/`masteryAtk2`/`statusAtk2` accumulators,
  `left_element = battle_get_weapon_element(..., EQI_HAND_L, ...)`, `patk` and
  `crit_atk_rate` left-hand branches, the `res` reduction on `wd.damage2`.

## Scope — every sub-system that must be touched

- [ ] Add a `leftHand` (or weapon-slot) parameter to `IBattleCardService.AddMastery`
      and resolve the off-hand weapon type for the left-hand mastery.
- [ ] Resolve the left weapon's element (`LeftWeaponElement`) once the `bonus bAtkEle`
      parser exists, or thread the lhw element from the item row; use it in
      `ComputeHandDamage` for the left hand.
- [ ] (If the full renewal accumulator split is pursued) model `statusAtk2`/`weaponAtk2`/
      `equipAtk2`/`masteryAtk2`/`patk`/`crit_atk_rate`/`res` per hand — but only as part
      of a combined right+left renewal-base-damage rewrite (coordinate with the trimmed
      `CalcBaseDamage` slice).

## Done criteria

- A dual-wielding Assassin whose off-hand is a different weapon type gets the off-hand
  mastery from the off-hand weapon, not the main hand.
- The off-hand element fix uses the left weapon's element when an endow/element is set.

## Test plan

- `Combat40LeftHandFidelityTests`: off-hand mastery resolves from the left weapon type;
  off-hand element fix uses the left weapon element when set.

## Notes / gotchas

- The AS_RIGHT/AS_LEFT/KO split itself (battle_calc_attack_left_right_hands) is DONE in
  COMBAT-18 and exact — this ticket is only the per-hand base-damage inputs (mastery +
  element) and the optional full accumulator rewrite.
