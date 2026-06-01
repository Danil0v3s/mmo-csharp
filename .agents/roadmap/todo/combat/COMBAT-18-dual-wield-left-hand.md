# COMBAT-18 — Dual-wield left-hand damage (battle_calc_attack_left_right_hands)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-04 · **Blocks:** none
> **Filed by:** COMBAT-04 (axis 4).

## Problem

Dual-wield (Assassin / Katar-less double-dagger, GS) deals only right-hand damage.
`BattleDamage.Damage2` exists but is always 0, and `EquipBonusAggregator` reads only
the right-hand weapon. rAthena `battle_calc_attack_left_right_hands` (battle.cpp:7150)
computes `damage2` from the left weapon and applies the mastery-based right/left split
and the renewal left-hand ATK reduction.

## Current state (C#)

- `Map.Server/Inventory/EquipBonusAggregator.cs:Aggregate` — reads only the right-hand
  weapon (`EquipRightHand`); no left-hand weapon ATK/level captured.
- `Map.Server/Combat/BattleCalculator.cs:CalcWeaponAttack` — `result.Damage2` never set.
- `Map.Server/Combat/DamageService.cs:BroadcastAct` — `Damage2 = 0` hardcoded.
- `EquipService` already resolves the left-hand slot (`EquipBits.HandL`).

## rAthena reference

- `battle.cpp:7150` `battle_calc_attack_left_right_hands` — right/left split + renewal
  left-hand reduction; single weapon → `damage2 = 0`.
- Left-hand mastery (`AS_LEFT`, `KO_*`) influences the split.

## Scope

- [ ] Capture the left-hand weapon (ATK + level) in `EquipSummary`/`PcBaseInputs`/
      `BattleStats` (mirror the right-hand fields COMBAT-04 added).
- [ ] Port `battle_calc_attack_left_right_hands` in `CalcWeaponAttack`: compute
      `Damage2` from the left weapon + the split/reduction.
- [ ] Thread `Damage2` through `BroadcastAct` (shares the wire with COMBAT-17).

## Done criteria

- A dual-wielding Assassin's auto-attack populates `Damage2 > 0`; single-weapon keeps
  `Damage2 = 0`; the split matches rAthena's left-hand reduction.

## Test plan

- Two weapons equipped → `Damage + Damage2`, left ≈ rAthena reduction.
- Single weapon → `Damage2 == 0`.
