# COMBAT-23 — pc_bonus single-value tail + 1-arg flag form (speed/healpower/nocastcancel/…)

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-06 · **Blocks:** none
> **Filed by:** COMBAT-06 (the single-value tail + flag form it scoped but didn't reach).

## Problem

Two gaps remain in the single-value `pc_bonus` coverage after COMBAT-06:
1. **The 1-arg flag form is not parsed at all.** `bonus bNoCastCancel;`,
   `bonus bUnbreakableArmor;`, `bonus bIntravision;`, etc. (no value) are dropped — the
   `BonusFlat` regex requires `bKey, N`.
2. **Single-value tail still missing**: `bSpeedRate`, `bHealPower`/`bHealPower2`,
   `bUseSPrate`, `bHPrecovRate`/`bSPrecovRate`, `bCriticalRate`, `bAddMaxWeight`,
   `bUnbreakable*` (flag), `bNoWeaponDamage`/`bNoMagicDamage`/`bNoMiscDamage`.

## Current state (C#)

- `Map.Server/Inventory/BonusScriptExtractor.cs:55-57` — `BonusFlat` regex needs a value;
  no flag-form regex. `ApplyFlat` lacks the above keys.
- `EquipBonusBundle` — no `SpeedRate`/`HealPower`/`UseSpRate`/recov-rate/flag bools.

## rAthena reference

- `pc.cpp:3644` `pc_bonus` arms incl. the flag-form ones (`SP_NO_CAST_CANCEL`,
  `SP_UNBREAKABLE_*`, `SP_INTRAVISION`). Consumers in `status.cpp` (speed_rate,
  heal_power, recov-rate, maxhp/maxweight), `skill.cpp` (nocastcancel → COMBAT-08).

## Scope

- [ ] Add a flag-form regex `bonus\s+b(?<key>[A-Za-z]+)\s*;` → `ApplyFlag(bundle, key)`
      setting bool fields (NoCastCancel, UnbreakableArmor/Weapon/…, Intravision, …).
- [ ] Extend `ApplyFlat`: `speedrate`, `healpower`/`healpower2`, `usesprate`,
      `hprecovrate`/`sprecovrate`, `criticalrate`, `addmaxweight`.
- [ ] Wire consumers: `speedrate` → `StatusCalcService` speed (note rAthena's max-of
      semantics); `healpower` → heal skill plugins; recov-rate → `NaturalHealService`;
      `nocastcancel` → COMBAT-08 cast-interrupt gate; maxweight → weight service.
- [ ] Route the flag/extended keys through `ScriptedBonusHost` too (shared
      `ApplyFlatBonus` entry).

## Done criteria

- `bonus bNoCastCancel;` parses and (with COMBAT-08) makes casts uninterruptible.
- `bonus bHealPower,30;` boosts Heal output 30%; `bonus bSpeedRate,25;` increases move
  speed per rAthena; recov-rate boosts natural regen.

## Test plan

- Flag-form parse test; each single-value consumer in isolation.
