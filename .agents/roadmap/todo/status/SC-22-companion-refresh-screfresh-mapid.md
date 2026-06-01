# SC-22 — P0.5 SC-engine remainder: companion calc refresh + status_change_refresh wiring + robust map-id lookup

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Split from:** SC-08

## Problem

SC-08 landed the spread + flags + Hermode/DeadlyDefeasance immune pieces. Three smaller P0.5
status.cpp leaves remain:

1. **Companion `status_calc_*` refresh** — `CalcHomunculus`/`CalcMercenary`/`CalcElemental` delegate
   to `CalcMob` + level override; a companion level-up / equip / SC change doesn't recompute the
   companion-specific derived stats (homun HpFactor/SpFactor, intimacy/hunger scaling; merc/elem db
   scaling). MaxHp doesn't grow on level-up.
2. **`status_change_refresh` weapon-swap call site** — `IStatusChangeService.Refresh` (End+Start the
   weapon-element SC family on weapon change) exists but has **no caller** (`grep '.Refresh('` finds
   only the definition). Swapping a weapon under Fireweapon/etc. doesn't re-resolve the element.
3. **`IsDisabledOnMap` map-id lookup** — uses `(uint)map.Name.GetHashCode() == mapId` (the
   codebase-wide convention, also in DamageService/MovementService). Low collision risk, but a
   robust `_world.GetById(mapId)` would remove the theoretical hazard.

## Current state (C#)

- `Map.Server/Status/StatusCalcService.cs` `CalcHomunculus`/`CalcMercenary`/`CalcElemental`.
- `Map.Server/Status/StatusChangeService.cs` `Refresh` (no caller) + `IsDisabledOnMap` (GetHashCode).
- Weapon-swap path: `IPlayerWeaponService.ChangeWeapon` (or equiv) should call `Refresh`.

## rAthena reference (source of truth)

- `status.cpp:2872` (homun), `:2887` (merc), `:2920` (elemental) — recompute from the companion db
  on level/equip change.
- `status.cpp status_change_refresh` — the weapon-element SC reapply set; called from
  `pc_calcweapontype` on weapon change.

## Scope

- [ ] Companion refresh: recompute homun/merc/elem derived stats on level-up/equip/SC (factor math
      from the companion db); a level-up grows MaxHp. (Coordinate with FEATURE-08/09/10.)
- [ ] Wire `Refresh` into the weapon-change path so the weapon-element SC family re-resolves.
- [ ] Replace the `GetHashCode` map-id lookup in `IsDisabledOnMap` with `_world.GetById(mapId)` (or
      the shared resolver), keeping the null-guard.

## Done criteria

- A homunculus/mercenary/elemental level-up refreshes MaxHp; weapon swap under a weapon-endow SC
  re-resolves the element; `IsDisabledOnMap` uses a collision-free map-id lookup.

## Test plan

- `CompanionCalcTests` (level-up MaxHp grows); weapon-swap Refresh test; nostatus map-id resolution.
