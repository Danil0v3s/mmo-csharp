# COMBAT-20 — Plant 1-damage + GvG/BG damage reductions

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
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

- [x] Plant predicate + clamp. Ported `is_infinite_defense`
      (`BattleCalculator.IsInfiniteDefense`: MD_IGNOREMELEE/RANGED by short/long lane,
      MD_IGNOREMAGIC, MD_IGNOREMISC) + `battle_calc_attack_plant`'s 1-damage clamp
      (`ApplyPlantAndZone`; katar off-hand → 0). Wired into the weapon auto-attack
      (`DamageService.PerformMeleeAttack`) and magic/misc (`CalcMagicAttack`/
      `CalcMiscAttack`). ➡️ Weapon-**skill** plant (post-ratio) + SC_INVINCIBLE moved to **COMBAT-42**.
- [x] GvG/BG stage. Rewrote the unused, buggy `ZoneDamageService` (was 25/75 rates +
      multiplied both range & type) into a faithful `Scale(lane, src, dmg, isSkill,
      isShort)`: GvG/BG flag → per-lane rate for skills (`{gvg,bg}_weapon/magic/misc`,
      default 60) vs short/long range rate for normals (default 80), via
      `IBattleConfigService`. Wired into the same auto-attack + magic/misc points;
      mutually exclusive with the plant clamp (rAthena returns before gvg_bg).
      ➡️ Weapon-skill zone + Emperium branch + INF2-ignore + can-hit gates + PK rate
      moved to **COMBAT-42**.

## Done criteria

- A non-ignoring hit on a plant-type mob deals exactly 1 ✅ (auto-attack + magic);
  an ignoring skill bypasses ✅ (magic vs melee-immune = full damage).
- On a GvG map a skill deals `gvg_damage_rate%` of its non-GvG value ✅ (magic skill = 60%).

## Test plan

- Plant clamp = 1; ignore-flag bypass. ✅
- GvG rate multiply with the map flag set vs unset. ✅

## History

- **2026-06-02** — inprogress→done. Plant 1-damage (`is_infinite_defense` +
  `battle_calc_attack_plant`) and GvG/BG zone scaling (`battle_calc_gvg/bg_damage`) now
  apply as a shared final stage (`BattleCalculator.ApplyPlantAndZone`) on the weapon
  auto-attack (`PerformMeleeAttack`) and magic/misc (`CalcMagic/MiscAttack`). Rewrote the
  dead, buggy `ZoneDamageService` into a faithful skill-vs-normal rate selector backed by
  `IBattleConfigService`. `Combat20PlantGvgTests` (14); unit suite 3802 (1 fail =
  pre-existing INFRA-11 replay gate). Filed COMBAT-42 (weapon-skill plant/zone post-ratio
  + Emperium/INF2-ignore/can-hit/PK/SC_INVINCIBLE gates).
