# COMBAT-77 — Res-ignore (by race + SC_A_TELUM / SC_POTENT_VENOM) on the physical Res reduction

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-61 (per-hand Res reduction) · **Blocks:** none
> **Filed by:** COMBAT-61 — the base `(5000+res)/(5000+10*res)` reduction landed; the
> ignore-res refinement that lowers the *effective* res before the curve is out of its scope.

## Problem

COMBAT-61 applied the renewal Res physical reduction
`damage = damage * (5000 + res) / (5000 + 10*res)` per hand (battle.cpp:7845), but uses the
raw target `res`. rAthena first reduces the effective res by the attacker's res-ignore
sources, so a character carrying `bonus2 bIgnoreResRace` (or under `SC_A_TELUM` /
`SC_POTENT_VENOM`) deals more physical damage to a high-Res target than the C# port does.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ComputeHandDamage` — applies the Res curve with raw
  `t.Res`; no `ignore_res` reduction. The `➡️ COMBAT-77` comment marks the spot.
- `Map.Server/Inventory/EquipBonusBundle.cs` — has no `IgnoreResRace`/`IgnoreResRaceAll`
  array (the bonus is parsed nowhere).
- `Map.Server/Inventory/BonusScriptExtractor.cs` — does not handle `bIgnoreResRace`.

## rAthena reference (source of truth)

- `battle.cpp:7843` (inside `battle_calc_weapon_attack`, renewal):
  ```c
  int16 res = tstatus->res;
  int16 ignore_res = 0;
  if (sd) ignore_res += sd->indexed_bonus.ignore_res_by_race[tstatus->race]
                      + sd->indexed_bonus.ignore_res_by_race[RC_ALL];
  if (sc) { if (sc->getSCE(SC_A_TELUM)) ignore_res += val2;
            if (sc->getSCE(SC_POTENT_VENOM)) ignore_res += val2; }
  ignore_res = min(ignore_res, battle_config.max_res_mres_ignored);   // default 50
  if (ignore_res > 0) res -= res * ignore_res / 100;
  wd.damage = wd.damage * (5000 + res) / (5000 + 10 * res);
  ```
- Switch caveat: canonical source is the monolithic `battle.cpp` body, not any split file.

## Scope — every sub-system that must be touched

- [ ] Add `int[] IgnoreResRace` (size = race count incl. RC_ALL) to `EquipBonusBundle` + reset.
- [ ] Parse `bonus2 bIgnoreResRace, r, n;` (and any `bIgnoreRes` all-race form) in
      `BonusScriptExtractor`.
- [ ] In `ComputeHandDamage`, compute `ignore_res` = race bonus + RC_ALL bonus + SC_A_TELUM.val2
      + SC_POTENT_VENOM.val2, clamp to the `max_res_mres_ignored` config (default 50), then
      `res -= res * ignore_res / 100` before the curve.
- [ ] Confirm `SC_A_TELUM` / `SC_POTENT_VENOM` exist in the SC engine with the right val2.

## Done criteria

- Against a Res-100 target, an attacker with `bIgnoreResRace` 50 vs that race takes the
  effective res to 50 → `damage * 5050/5500`, matching rAthena.
- Clamp at `max_res_mres_ignored` honored.

## Test plan

- Numeric test: raw res vs res-after-ignore (race bonus, RC_ALL, SC val2, clamp).

## Notes / gotchas

- `MRes` has the identical pattern on the magic side (battle.cpp:9300) — out of scope here,
  but mirror the design if you touch it.
