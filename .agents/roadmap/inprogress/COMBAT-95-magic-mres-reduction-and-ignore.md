# COMBAT-95 — Magic-side MRes reduction curve + ignore (by race + SC_A_VITA)

> **Epic:** combat · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-77 (the physical Res-ignore design to mirror) · **Blocks:** none
> **Filed by:** COMBAT-77 — it added the physical Res reduction's ignore_res (race + SC). The
> magic lane's identical MRes reduction is not modeled in C# **at all** (no curve, no ignore).

## Problem

rAthena applies an MRes trait-stat reduction to MAGIC damage, mirroring the physical Res curve:
`ad.damage = ad.damage * (5000 + mres) / (5000 + 10*mres)`, with the effective MRes first lowered
by the attacker's ignore sources. The C# magic lane (`BattleCalculator.CalcMagicAttack`) has
**neither** the curve nor the ignore — high-MRes targets take full magic damage, and
`bIgnoreMResRace` / `SC_A_VITA` do nothing.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:CalcMagicAttack` — no `(5000+mres)/(5000+10*mres)` term.
- `Map.Server/Inventory/EquipBonusBundle.cs` — no `IgnoreMResRace` array (COMBAT-77 added the
  physical `IgnoreResRace`; mirror it).
- `SC_A_VITA` — confirm it exists in the SC engine with the MRes-pierce val2.

## rAthena reference (source of truth)

- `battle.cpp:9290-9305` (renewal magic path):
  ```c
  int16 mres = tstatus->mres;
  int16 ignore_mres = 0;
  if (sd) ignore_mres += sd->indexed_bonus.ignore_mres_by_race[tstatus->race]
                       + sd->indexed_bonus.ignore_mres_by_race[RC_ALL];
  if (sc && sc->getSCE(SC_A_VITA)) ignore_mres += sc->getSCE(SC_A_VITA)->val2;
  ignore_mres = min(ignore_mres, battle_config.max_res_mres_ignored);
  if (ignore_mres > 0) mres -= mres * ignore_mres / 100;
  ad.damage = ad.damage * (5000 + mres) / (5000 + 10 * mres);
  ```
- Mirror COMBAT-77's physical implementation (the `MaxResMresIgnored` const is already shared).

## Scope — every sub-system that must be touched

- [ ] Add `int[] IgnoreMResRace` to `EquipBonusBundle` (+ Reset) and parse `bonus2 bIgnoreMResRace`
      in `ApplyIndexed` (mirror `ignoreresrace`).
- [ ] In `CalcMagicAttack`, apply the MRes reduction with the effective-MRes lowering (race +
      RC_ALL + SC_A_VITA.val2, clamped to `MaxResMresIgnored`) at the rAthena order point.
- [ ] Confirm `SC_A_VITA` exists with the right val2 (else register it).

## Done criteria

- A magic hit on an MRes-100 target is reduced by `(5000+mres)/(5000+10*mres)`; an attacker with
  `bIgnoreMResRace` / `SC_A_VITA` raises it by the (clamped) ignore %, matching rAthena.

## Test plan

- `Combat95MResTests`: raw mres curve + race/RC_ALL/SC_A_VITA ignore + clamp (mirror Combat77).

## Notes / gotchas

- Reuse `BattleCalculator.MaxResMresIgnored` (COMBAT-77) — same battle_config cap for both lanes.
