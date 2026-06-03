# COMBAT-29 — Dual-wield + shield ASPD base terms

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-09 (base renewal ASPD formula) · **Blocks:** none

## Problem

The renewal ASPD base (`status_base_amotion_pc`) starts from `job->aspd_base[weapontype1]`
and then adds the **shield** weapon-base row when a shield is equipped, or a **dual-wield**
`aspd_base[weapontype2] / 4` term when both hands hold different weapons. COMBAT-09
implemented only the single-weapon path: `PcBaseInputs` carries one `WeaponType`, and the
`EquipSummary` exposes neither the second-hand weapon type nor a shield flag. So a
shield wearer / dual-wielder gets the wrong base ASPD.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `RenewalPcAmotion` — uses a single `aspdBase`
  from `_jobAspd.GetBaseAspdByJobId(JobId, WeaponType)`; no shield / second-weapon term.
- `Map.Server/Status/IStatusCalcService.cs` `PcBaseInputs` — single `WeaponType`, no
  `WeaponType2` / `HasShield`.
- `Map.Server/Inventory/EquipBonusAggregator.cs` `EquipSummary` — no second-weapon-type or
  shield field. `EquipBits.ShadowShield` / the `EquipShield` bit exist; `PlayerEquipHelpers.
  CalcWeaponType` already knows shield vs dual-wield but only stores the single
  `pc.WeaponType`.

## rAthena reference (source of truth)

- `status.cpp:2321-2325`:
  ```c
  int32 aspd = job->aspd_base[sd->weapontype1];
  if (sd->status.shield)                aspd += job->aspd_base[MAX_WEAPON_TYPE];   // shield row
  else if (sd->weapontype2 != W_FIST && hand_r != hand_l)
                                          aspd += job->aspd_base[sd->weapontype2] / 4;  // dual-wield
  ```
- `job_aspd.yml` ships a `Shield` weapon row (index `MAX_WEAPON_TYPE`, the C# importer maps
  `Shield → 99`).

## Scope — every sub-system that must be touched

- [x] `WeaponType2` already exists as `PcBaseInputs.LeftWeaponType` (COMBAT-18); added
      `HasShield` to `PcBaseInputs` + `BattleStats` (default false).
- [x] `EquipSummary.HasShield` captured in `Aggregate` (an off-hand `Type == "Armor"` row);
      threaded through all 4 `CalcPc` builders (EquipService, NotifyActorInitHandler from the
      summary; StatusOpsService, ExpService from `pc.Stats`).
- [x] In `CalcPc` (where `_jobAspd` is available) the shield base
      (`aspd_base[Shield=99]`) is added when `HasShield`, else the dual-wield
      `aspd_base[weaponType2]/4` when the off-hand holds a distinct weapon. New
      `IJobAspdCacheService.GetBaseAspdExactByJobId` returns the exact row or 0 (no
      fist/default fallback) so a missing row adds nothing.

## Done criteria

- Equipping a shield raises base amotion by the job's `Shield` ASPD row ✅.
- Dual-wielding a distinct off-hand weapon raises base amotion by `aspd_base[secondWeapon]/4` ✅.
- Single-weapon, no-shield ASPD is unchanged from COMBAT-09 ✅.

## Test plan

- Unit-test `RenewalPcAmotion` with a shield base and with a dual-wield second weapon;
  assert the added terms.
- Integration via CalcPc with a shield-equipped summary.

## Notes / gotchas

- Overlaps the dual-wield damage work (COMBAT-18) only in that both need to know the
  left-hand weapon — coordinate the `WeaponType2` plumbing so it lands once.

## History

- **2026-06-02** — inprogress→done. `CalcPc` now adds the renewal shield / dual-wield ASPD
  base terms: `+ aspd_base[Shield=99]` when a shield is worn (new `EquipSummary.HasShield`
  from an off-hand Armor row + `PcBaseInputs`/`BattleStats.HasShield` threaded through all 4
  recalc builders), else `+ aspd_base[wt2]/4` reusing COMBAT-18's `LeftWeaponType`. New
  `IJobAspdCacheService.GetBaseAspdExactByJobId` (exact row or 0) keeps a missing row from
  adding the "no row" default. Combat29AspdShieldDualWieldTests (2); unit suite 3845 (1 fail
  = pre-existing INFRA-11 replay gate). No follow-ups — all done criteria met.
