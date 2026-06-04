# SC-IMMUNE — Immunity matrix + refresh/spread wiring

> **Epic:** status · **Status:** ✅ Done (2026-06-04) · **Size:** S · **Player-visible:** yes
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

- [x] PC card-bonus effect-resist matrix (`bResEff`) wired into the `GetScDef` resist pipeline (turn 2):
      new `EquipBonusBundle.ResEff` (per-StatusType %), parsed from `bonus2 bResEff, eff, n`
      (`BonusScriptExtractor` + `ParseEffSc`), and applied in `GetScDef` to the rate (and, renewal, the
      duration) — rAthena `sd->reseff` / `status_get_sc_def`.
- [x] `status_change_refresh` weapon-swap wiring (turn 1): `EquipService.TryRecalcStats` now calls
      `IStatusChangeService.Refresh` after the equip `CalcPc` (re-resolves the Fire/Earth/Wind/Waterweapon
      endow SCs on a weapon swap; the method existed but had no caller).
- [x] Robust `nostatus` map-id lookup (turn 1): `StatusChangeService.IsDisabledOnMap` now resolves the
      map via a once-built `mapId → name` cache instead of the per-call O(N) `GetHashCode` linear scan.
- [ ] Companion (homun/merc/elem) `status_calc_*` refresh — a level-up recomputes the companion-specific
      derived stats (MaxHp grows). ➡️ Moved to **SC-COMPANION-CALC** (genuinely blocked: needs the
      companion db factors + the level-up trigger, which are GP-HOMUN/GP-MERC/GP-ELEM — still in `todo`).

## Done criteria

- ✅ A player with effect-resist cards resists the matching SC by the card amount
  (`Skill01ScDefTests.EffectResistCard_ReducesScRateAndDuration` / `_OnlyAffectsTheMatchingSc` /
  `BResEff_bonus_parses_into_the_reseff_map`).
- ✅ `nostatus` maps block the listed SCs (functional; now via the cached map-id lookup).
- The weapon-swap status_change_refresh re-resolves the endow element
  (`EquipWeapon_RefreshesWeaponElementStatuses`).
- Companion (homun/merc/elem) calc refresh on level-up ➡️ **SC-COMPANION-CALC** (blocked on
  GP-HOMUN/GP-MERC/GP-ELEM).

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
- **2026-06-04 (turn 2 — DONE)** — The `bResEff` effect-resist card matrix. New
  `EquipBonusBundle.ResEff` (StatusType → 1/100% resist), parsed from `bonus2 bResEff, eff, n` via
  `BonusScriptExtractor.ApplyIndexedBonus`'s new `reseff` case + `ParseEffSc` (Eff_/numeric/name →
  StatusType). `GetScDef` reads it (`ResEffFor`) and cuts the SC landing rate (`rate -= rate*res/10000`,
  before the Aegis rounding) and — renewal — the duration, mirroring rAthena `status_get_sc_def`'s
  `sd->reseff` loop. 3 tests (rate+duration cut, only-matching-SC, the bonus parse); full suite 4555 pass
  (1 = standing replay-fixture). The companion calc refresh is genuinely blocked on GP-HOMUN/MERC/ELEM →
  filed **SC-COMPANION-CALC**; the other two leaves landed in turn 1 → **DONE**.

## History

- 2026-06-04 — Done. Turn 1: weapon-swap `status_change_refresh` wiring + cached nostatus map-id lookup.
  Turn 2: the `bResEff` effect-resist card matrix in `GetScDef`. Companion calc refresh ➡️
  SC-COMPANION-CALC (blocked on the companion lifecycle tickets).

## Notes

- Completes the SC-08 P0.5 leaves.
