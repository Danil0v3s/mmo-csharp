# COMBAT-23 — pc_bonus single-value tail + 1-arg flag form (speed/healpower/nocastcancel/…)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Flag-form regex `bonus bX;` → `ApplyFlag`: sets `NoCastCancel`,
      `Unbreakable{Armor,Weapon,Helm,Shield,Shoes,Garment}`, `Intravision`. Ordered after
      the valued matcher (the `;`-terminated regex never matches a valued `bonus bX,N;`).
- [x] Extended `ApplyFlat`: `healpower`/`healpower2`, `hprecovrate`/`sprecovrate`,
      `speedrate` (stored as `min(-val)` per SP_SPEED_RATE), `speedaddrate`, `criticalrate`,
      `usesprate`, `addmaxweight`. Bundle fields + `Reset()`.
- [x] Wired consumers: `healpower` → `Heal.CalcRenewalHeal`; recov-rate → `NaturalHealService`
      (HP + SP regen ×(100+rate)/100, floor 1); `nocastcancel` → COMBAT-08 gate (already wired).
      ➡️ `speedrate` (needs a status_calc_speed port), crit-rate / usesp / max-weight /
      heal-power2 / unbreakable consumers moved to **COMBAT-45**.
- [x] Routed the flag form through `ScriptedBonusHost.bonus(1-arg)` → `ApplyFlagBonus`
      (was a silent skip).

## Done criteria

- `bonus bNoCastCancel;` parses and (with COMBAT-08) makes casts uninterruptible ✅.
- `bonus bHealPower,30;` boosts Heal output 30% ✅; recov-rate boosts natural regen ✅;
  ➡️ `bonus bSpeedRate,25;` move-speed consumer moved to **COMBAT-45** (the C# PC speed is
  a flat 150 — needs a status_calc_speed port).

## Test plan

- Flag-form parse test ✅ (+ a guard that a valued `bonus bAtk,10` isn't mis-read as a flag);
  each single-value consumer ✅ (HealPower heal output, recov-rate regen math, parse of the tail).

## History

- **2026-06-02** — inprogress→done. Added the 1-arg flag-form parser (`bNoCastCancel` +
  the `bUnbreakable*` / `bIntravision` flags) + the single-value tail (`bHealPower`,
  `bHPrecovRate`/`bSPrecovRate`, `bSpeedRate`(min -val), `bCriticalRate`, `bUseSPrate`,
  `bAddMaxWeight`) to `EquipBonusBundle` + the extractor + the V8 `ScriptedBonusHost`.
  Wired `HealPower` into the renewal heal formula and the recov-rates into
  `NaturalHealService`. `Combat23PcBonusTailTests` (7); unit suite 3821 (1 fail =
  pre-existing INFRA-11 replay gate). Also fixed a pre-existing `ElementTable`
  static-seed race between the element test classes (seed the superset). Filed COMBAT-45
  (speed/crit/usesp/maxweight/healpower2/unbreakable consumers).
