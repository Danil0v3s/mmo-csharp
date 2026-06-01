# SC-21 — status_isimmune PC card-bonus tolerance matrix (bAddDefRate / bAddRaceTolerance / …)

> **Epic:** Status parity hardening · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-06 (bonus parse) · **Split from:** SC-08

## Problem

SC-08 added the Hermode (→immune) / DeadlyDefeasance (→strips immunity) bits to
`StatusOpsService.IsImmune`. The PC card-bonus *resistance/tolerance matrix* —
`bAddDefRate`, `bAddItemHealRate`, `bAddRaceTolerance` and the per-race/element/size
tolerance multipliers — is NOT applied to incoming damage/heal. These live in the equip-bonus
pipeline, not the SC engine, so they were out of SC-08's scope.

## Current state (C#)

- `Map.Server/Status/StatusOps/StatusOpsService.cs` `IsImmune` — Hermode/DeadlyDefeasance + mob
  MD_STATUSIMMUNE only (comment cites this ticket).
- `Map.Server/Inventory/EquipBonusBundle.cs` / `BonusScriptExtractor` — the tolerance fields
  (bAddRaceTolerance etc.) are not all parsed/applied.

## rAthena reference (source of truth)

- `pc.cpp` `pc_bonus` `SP_ADDDEFRATE` / `SP_ADD_ITEM_HEAL_RATE` / `SP_SUBRACE` (tolerance) etc.
- `battle.cpp` tolerance reads apply the multipliers to incoming damage / heal.

## Scope

- [ ] Parse + store the tolerance bonuses in `EquipBonusBundle` (coordinate with COMBAT-06/21).
- [ ] Apply them in the incoming damage / heal path (`DamageService` / heal resolver).

## Done criteria

- A card granting bAddRaceTolerance(Demon, 20) reduces incoming Demon damage by 20%; bAddDefRate
  raises hard DEF per rAthena; bAddItemHealRate boosts potion heal.

## Test plan

- Per-bonus damage/heal tests with the card bonus set.

## Notes

- This is the equip-pipeline half of status_isimmune that SC-08 explicitly left out.
