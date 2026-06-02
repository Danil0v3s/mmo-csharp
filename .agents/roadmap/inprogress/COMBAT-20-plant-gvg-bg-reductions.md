# COMBAT-20 — Plant 1-damage + GvG/BG damage reductions

> **Epic:** Combat parity · **Status:** 🚧 In progress · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-05 (axis 5).

## Problem

Two final-stage reductions are missing:
1. **Plant 1-damage.** `battle_calc_attack_plant` (battle.cpp:7074) clamps damage to 1
   against plant-type targets (unless the skill ignores it). Not ported — plants take
   full damage.
2. **GvG / BG rate.** `battle_calc_attack_gvg_bg` (battle.cpp:7225) multiplies damage
   by the configured rate on WoE (GvG) / Battleground maps. Not ported.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs` def stage — no plant clamp.
- `Map.Server/Combat/DamageService.cs` — resolves map flags (`:136-143`) and applies SC
  reductions, but no GvG/BG % stage.
- `IMapFlagService` exposes map flags; battle_config GvG/BG rates need a config source.

## rAthena reference

- `battle.cpp:7074` `battle_calc_attack_plant` — verify the exact plant condition
  (mode flag / class) before clamping; ignoring skills bypass.
- `battle.cpp:7225` `battle_calc_attack_gvg_bg` — `gvg_damage_rate`, `gvg_weapon/long/
  magic`, and the BG equivalents.

## Scope

- [ ] Read the exact rAthena "plant" predicate; clamp final damage to 1 for plant
      targets (after element, before min-damage), honoring skill ignore flags.
- [ ] Add a GvG/BG stage keyed off the source map's `MapFlag.Gvg`/`Battleground`,
      multiplying by the configured rate (wire battle_config rates).

## Done criteria

- A non-ignoring hit on a plant-type mob deals exactly 1; an ignoring skill bypasses.
- On a GvG map a skill deals `gvg_damage_rate%` of its non-GvG value.

## Test plan

- Plant clamp = 1; ignore-flag bypass.
- GvG rate multiply with the map flag set vs unset.
