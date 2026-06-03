# COMBAT-95 — Magic-side MRes reduction curve + ignore (by race + SC_A_VITA)

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
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

- [x] Added `int[] IgnoreMResRace` to `EquipBonusBundle` (+ `Reset` clear) and the
      `bonus2 bIgnoreMResRace` parse in `BonusScriptExtractor` (mirrors `ignoreresrace`).
- [x] In `CalcMagicAttack`, apply the MRes reduction `damage * (5000+mres)/(5000+10*mres)` with the
      effective-MRes lowering (`IgnoreMResRace[race]` + `[RC_ALL]` PC-equip + `SC_A_VITA.val2`,
      clamped to `MaxResMresIgnored`), placed BEFORE the MDEF block (rAthena order, battle.cpp:9278:
      "calculated before MDEF"). Outside any sd-gate so NPC magic is reduced too.
- [x] SC_A_VITA was registered presence-only — changed it to materialize `Val2 = 5*Val1`
      (Res/MRes pierce %, status.cpp:12471; mirrors SC_A_TELUM) so the combat reader sees it.

## Done criteria

- ✅ A magic hit on an MRes-100 target is reduced by `(5000+mres)/(5000+10*mres)` (100 → 85); an
  attacker with `bIgnoreMResRace` (race or RC_ALL) / `SC_A_VITA` raises it by the (clamped) ignore %
  (→ 91 at 50% ignore), matching rAthena. Verified by `Combat95MResTests`.

## Test plan

- ✅ `Combat95MResTests` (7): raw mres curve (85 / no-MRes passthrough) + race/RC_ALL/SC_A_VITA
  ignore (→ 91) + sum-before-clamp + clamp-to-50 + the SC_A_VITA `Val2 = 5*Val1` registration.

## Notes / gotchas

- Reused `BattleCalculator.MaxResMresIgnored` (COMBAT-77, = 50) — same battle_config cap for both lanes.

## History

- 2026-06-03 — Ported the renewal MRes magic reduction to `CalcMagicAttack` (mirror of COMBAT-77's
  physical Res): `damage * (5000+mres)/(5000+10*mres)` before MDEF, with the effective MRes lowered
  by `bonus2 bIgnoreMResRace[race]+[RC_ALL]` (new `EquipBonusBundle.IgnoreMResRace` + extractor case)
  and `SC_A_VITA.val2`, clamped to 50. Materialized SC_A_VITA's `Val2 = 5*Val1` (was presence-only).
  `Combat95MResTests` (7); full Map.Server.Tests 4248 pass (1 fail = pre-existing INFRA-11 replay
  gate). No follow-ups.
