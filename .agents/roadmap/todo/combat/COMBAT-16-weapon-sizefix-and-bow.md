# COMBAT-16 — Weapon size-fix table + bow arrow_atk

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-04 · **Blocks:** none
> **Filed by:** COMBAT-04 (axis 2 + the bow slice it scoped out).

## Problem

1. **Per-weapon size penalty (`atkmods`) not applied.** `BattleCalculator.SizeMod`
   returns 100 for every size. In this **renewal** server the size penalty is tiny
   (`db/re/size_fix.yml`): only **Knuckle** and **Whip** take **75%** vs **Large**;
   every other weapon/size is 100%. So the stub is correct for ~all weapons but
   wrong for Knuckle/Whip vs Large.
2. **Bow `arrow_atk`.** The PC base-damage path (COMBAT-04) does not add the equipped
   arrow's ATK, nor take the bow atkmin/atkmax from arrow + weapon. Ammo consumption
   on attack is also unported.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs` — `SizeMod(targetSize)` returns 100 always;
  called at the PC size-mod multiply.
- Weapon **type** is not on `BattleStats` (only `WeaponLevel` was added in COMBAT-04);
  `PcBaseInputs.WeaponType` exists but is never populated.
- No arrow/ammo concept in the equip aggregator or `CalcBaseDamage`.

## rAthena reference

- `db/re/size_fix.yml` — Knuckle/Whip Large 75; all else default 100. (Pre-renewal
  has many more, but this server is renewal-only.)
- `battle.cpp:2453` `battle_calc_base_damage` — `t_size` indexes the weapon's
  `atkmods[]`; bow path adds `arrow_atk` and derives atkmin/atkmax from arrow+weapon.

## Scope

- [ ] Plumb the equipped weapon **type** (from `ItemEntity.Subtype`) → `EquipSummary`
      → `PcBaseInputs.WeaponType` → a `BattleStats.WeaponType` field.
- [ ] `SizeMod`: return 75 for Knuckle/Whip vs Large, else 100 (bake the renewal
      `size_fix.yml`; bare-handed = 100/100/100).
- [ ] Bow: when the weapon is a bow, add the equipped arrow's ATK to the swing and
      derive atkmin/atkmax per rAthena; consume ammo on attack.

## Done criteria

- A Knuckle/Whip user vs a Large target deals 75% of the vs-Small damage; every other
  weapon is unaffected (renewal).
- A bow + arrow attack includes `arrow_atk`; firing consumes one arrow.

## Test plan

- `SizeMod` unit: Knuckle/Whip×Large=75, sword×Large=100, bare=100.
- Bow swing includes arrow atk; ammo decrements.
