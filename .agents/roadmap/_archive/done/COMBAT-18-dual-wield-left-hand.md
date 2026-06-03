# COMBAT-18 — Dual-wield left-hand damage (battle_calc_attack_left_right_hands)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Capture the left-hand weapon (ATK + level + type + element) in
      `EquipSummary`/`PcBaseInputs`/`BattleStats` (mirror the right-hand COMBAT-04
      fields). `EquipBonusAggregator` reads `EquipLeftHand` rows where `Type == "Weapon"`
      (a shield in the same slot is excluded). Threaded through all 4 recalc-input
      builders (EquipService, NotifyActorInitHandler, StatusOpsService, ExpService).
- [x] Port `battle_calc_attack_left_right_hands` in `CalcWeaponAttack`: a shared
      `ComputeHandDamage` runs both hands through the identical pipeline; `Damage2` from
      the left weapon; `ApplyLeftRightSplit` applies the katar off-hand fraction
      (`damage*(1+2*TF_DOUBLE)/100`) and the thief AS_RIGHT/AS_LEFT (50+10·lv / 30+10·lv)
      + kagerou KO_RIGHT/KO_LEFT (70+10·lv / 50+10·lv) masteries with floors, gated on
      `is_attack_right/left_handed`.
      ➡️ Per-hand mastery weapon + per-hand left element + the full renewal accumulator
      split (`statusAtk2`/`weaponAtk2`/`patk`/`crit_atk_rate`/`res`) moved to **COMBAT-40**.
- [x] Thread `Damage2` through `BroadcastAct` (`ApplyResolved`/`BroadcastAct` carry
      `damage2`; the wire `Damage` is the right-hand remainder, `Damage2` the off-hand).

## Done criteria

- A dual-wielding Assassin's auto-attack populates `Damage2 > 0` ✅; single-weapon keeps
  `Damage2 = 0` ✅; the split matches rAthena's left-hand reduction ✅ (AS_LEFT/AS_RIGHT
  + katar formula verified against the exact percentages).

## Test plan

- Two weapons equipped → `Damage + Damage2`, left ≈ rAthena reduction. ✅
- Single weapon → `Damage2 == 0`. ✅

## History

- **2026-06-02** — inprogress→done. Off-hand weapon (lhw) captured by
  `EquipBonusAggregator` (weapon-only off-hand slot) → `BattleStats.LeftWatk*` via all 4
  recalc-input builders; `BattleCalculator.ComputeHandDamage` shares the renewal pipeline
  across both hands; `ApplyLeftRightSplit` ports `battle_calc_attack_left_right_hands`
  exactly (katar TF_DOUBLE fraction + AS_RIGHT/AS_LEFT + KO_RIGHT/KO_LEFT, floors, hand
  gates); `Damage2` threads `PerformMeleeAttack` → `ApplyResolved` → `BroadcastAct` →
  `ZC_NOTIFY_ACT3.Damage2`. Added skill ids AS_RIGHT/AS_LEFT/KO_RIGHT/KO_LEFT.
  `Combat18DualWieldTests` (8) green; Map.Server unit suite 3779 (the 1 fail is the
  pre-existing INFRA-11 replay E2E gate). Filed COMBAT-40 (per-hand mastery/element +
  full renewal accumulator fidelity).
