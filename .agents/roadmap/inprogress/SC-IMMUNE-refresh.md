# SC-IMMUNE — Immunity matrix + refresh/spread wiring

> **Epic:** status · **Status:** 🚧 In progress · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Status-immunity (card-bonus tolerance matrix) + the companion calc-refresh + `status_change_refresh`
> + robust `nostatus` map-id lookup all behave per rAthena.

## What this absorbs (archive)

- `_archive/todo/status/SC-21` — `status_isimmune` PC card-bonus tolerance matrix.
- `_archive/todo/status/SC-22` — companion calc refresh + `status_change_refresh` wiring + robust `nostatus` map-id lookup.

## rAthena reference

- `rathena/src/map/status.cpp` — `status_isimmune` (the per-eff tolerance from card bonuses),
  `status_change_refresh`, the `nostatus` mapflag gate.

## Scope

- [ ] PC card-bonus status-tolerance matrix (`bResEff` effect-resist) wired into the
      `GetScDef` resist pipeline. **→ turn 2.**
- [~] Companion `status_calc_*` refresh + `status_change_refresh` wiring.
      - [x] `status_change_refresh` weapon-swap wiring (turn 1): `EquipService.TryRecalcStats` now calls
            `IStatusChangeService.Refresh` after the equip `CalcPc` (re-resolves the weapon-element endow
            SC family Fire/Earth/Wind/Waterweapon on a weapon swap; the method existed but had no caller).
      - [ ] Companion (homun/merc/elem) calc refresh — a level-up recomputes the companion-specific
            derived stats (MaxHp grows). **→ turn 2/3.**
- [x] Robust `nostatus` map-id lookup (turn 1): `StatusChangeService.IsDisabledOnMap` now resolves the
      map via a once-built `mapId → name` cache instead of the per-call O(N) `GetHashCode` linear scan.

## Done criteria

- A player with effect-resist cards resists the matching SC by the card amount (turn 2); companions
  refresh their calc on the right triggers (weapon-swap ✅ turn 1; companion level-up → turn 2/3);
  `nostatus` maps block the listed SCs (✅ — functional, now via the cached lookup).

## Test plan

- Extend the archived SC-21/22 tests.

## Progress log (multi-turn)

- **2026-06-04 (turn 1)** — The two cleanest pieces. (a) `status_change_refresh` weapon-swap wiring:
  injected `IStatusChangeService` (optional, cycle-free) into `EquipService` and call `Refresh(player)`
  after the equip recalc, so a weapon swap under a Fire/Earth/Wind/Waterweapon endow re-resolves the
  element. (b) Robust nostatus map-id lookup: `IsDisabledOnMap` uses a lazily-built `mapId → name` cache
  (O(1)) instead of the per-call linear `GetHashCode` scan. Tests: `EquipWeapon_RefreshesWeaponElementStatuses`
  (+ a `RefreshCalls` counter on the shared `RecordingStatusChangeService`); full suite 4552 pass (1 =
  standing replay-fixture). **Remaining (turn 2/3):** the `bResEff` effect-resist card matrix in `GetScDef`,
  and the companion (homun/merc/elem) calc refresh on level-up. The loop resumes this card.

## Notes

- Completes the SC-08 P0.5 leaves.
