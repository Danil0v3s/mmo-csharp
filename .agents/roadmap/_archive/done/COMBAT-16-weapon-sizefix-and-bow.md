# COMBAT-16 — Weapon size-fix table + bow arrow_atk

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] **Plumb the equipped weapon type** — ✅ found TWO compounding bugs:
      `item_db.SubType` is a NAME ("Knuckle"/"Bow"/…) so the old `int.TryParse`
      always yielded 0, AND `CalcWeaponType` was never called. New
      `WeaponTypeCodes` (name→`W_*`); `EquipBonusAggregator.Aggregate` now resolves
      the right-hand `WeaponType` and surfaces it on `EquipSummary`; EquipService +
      NotifyActorInitHandler set `player.WeaponType` from it (also fixes the renewal
      ASPD job_aspd lookup, previously stuck on Fist). Combat reads
      `PlayerEntity.WeaponType` (no `BattleStats` field needed).
- [x] **SizeMod** — ✅ renewal `size_fix.yml`: `BattleCalculator.SizeMod(weaponType,
      size)` returns 75 only for Knuckle/Whip vs Large, else 100 (bare = 100).
- [x] **Bow arrow_atk** — ✅ the aggregator folds the equipped ammo's ATK into the
      swing for `WeaponTypeCodes.UsesAmmo` weapons (Bow + guns); non-ammo weapons
      (incl. Musical/Whip) ignore equipped ammo. **Ammo consumption on attack +
      the no-ammo gate ➡️ COMBAT-36** (attack-loop concern, not damage math).

## Done criteria

- ✅ A Knuckle/Whip user vs a Large target deals 75% of the vs-Small damage; every
  other weapon is unaffected (`Combat16WeaponSizeBowTests.SizeMod_*`,
  `WeaponTypeCodes_resolve_*`).
- ◑ A bow + arrow attack includes `arrow_atk` (✅ `Aggregate_resolves_..._arrow_atk`);
  firing consumes one arrow ➡️ **COMBAT-36**.

## Test plan

- `SizeMod` unit: Knuckle/Whip×Large=75, sword×Large=100, bare=100.
- Bow swing includes arrow atk; ammo decrements.

## History

- 2026-06-02 · Renewal size-fix + weapon-type resolution + bow arrow_atk. Discovered
  WeaponType was always 0 (item_db SubType is a NAME, old int.TryParse failed; and
  CalcWeaponType was never called) — added WeaponTypeCodes (name→W_*), resolved it in
  EquipBonusAggregator, and set player.WeaponType in EquipService + at login (also
  un-breaks the renewal ASPD job_aspd lookup). SizeMod now applies the renewal
  Knuckle/Whip ×Large=75 (else 100). Aggregator folds equipped ammo ATK into the swing
  for Bow/gun weapons. Combat16WeaponSizeBowTests (17); suite 3761 green. Filed
  COMBAT-36 (ammo consumption + no-ammo gate — the attack-loop half of the bow slice).
