# COMBAT-42 — Weapon-skill plant/zone + GvG gates (Emperium / INF2-ignore / PK)

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-20 (plant + zone base), SKILL-05 (ComputeSkillDamage funnel)
> **Blocks:** none
> **Filed by:** COMBAT-20 — the post-ratio weapon-skill path + the GvG can-hit/ignore gates it deferred.

## Problem

COMBAT-20 wired the plant 1-damage clamp + GvG/BG zone scaling for the weapon
**auto-attack** (`DamageService.PerformMeleeAttack`) and for **magic/misc** skills
(`CalcMagicAttack`/`CalcMiscAttack`, which carry the full damage). Weapon **skills**
(Bash, Sonic Blow, …) compute their final damage post-ratio in
`WeaponSkillImpl.ComputeSkillDamage` / the `SkillAttackService` funnel, which do NOT
run the plant/zone stage — so a Bash on a Flora deals full damage, and a weapon skill
in WoE is not GvG-reduced. Several GvG gates from rAthena are also unported.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs:ApplyPlantAndZone` — the shared final stage;
  called only from `PerformMeleeAttack` (auto) and `CalcMagic/MiscAttack`.
- `Map.Server/Skills/Behaviors/SkillImpl.cs:WeaponSkillImpl.ComputeSkillDamage` /
  `Map.Server/Skills/SkillAttackService.cs` — final weapon-skill damage; no plant/zone.
- `Map.Server/Combat/ZoneDamageService.cs` — GvG/BG rate only; no can-hit gate, no
  INF2 ignore-reduction, no PK rate.
- `IsInfiniteDefense` does not check the target's SC_INVINCIBLE (battle.cpp:2844).

## rAthena reference (source of truth)

- `battle.cpp:8000` — weapon attack applies `battle_calc_attack_plant` (infdef) OR
  `battle_apply_div_fix` + `battle_calc_attack_left_right_hands` + `battle_calc_attack_gvg_bg`.
- `battle.cpp:7104-7118` — Emperium branch inside `battle_calc_attack_plant`
  (`battle_can_hit_gvg_target` + `battle_calc_gvg_damage`).
- `battle.cpp:2051/2126` `battle_can_hit_bg/gvg_target` — return 0 damage when the
  target can't be hit; `INF2_IGNOREGVGREDUCTION`/`INF2_IGNOREBGREDUCTION` bypass.
- `battle.cpp:2158` `battle_calc_pk_damage` — pk_mode rate (PC↔PC).
- `battle.cpp:2844` `is_infinite_defense` — SC_INVINCIBLE → plant.

## Scope — every sub-system that must be touched

- [x] Apply the plant/zone stage at the weapon-skill final point — new
      `IBattleCalculator.ApplyWeaponSkillPlantZone` (wraps the post-ratio total in a
      weapon-lane `BattleDamage` and runs `ApplyPlantAndZone`), called from
      `WeaponSkillImpl.CastendDamageId` (melee/short default) and the
      `SkillAttackService` weapon funnel (BF_LONG when `_db.GetRange(skillId) > 3`).
- [x] SC_INVINCIBLE branch in `IsInfiniteDefense` — threaded `IStatusChangeService`
      through `ApplyPlantAndZone`/`IsInfiniteDefense`; the auto-attack + magic/misc +
      weapon-skill paths all pass it. (Live behavior is dormant where
      `BattleCalculator._sc` is null — COMBAT-59.)
- [x] `INF2_IGNOREGVGREDUCTION`/`INF2_IGNOREBGREDUCTION` bypass + `can_hit_gvg/bg`
      gate ➡️ COMBAT-62 (data-blocked: `SkillInf2` lacks the flags and `Inf2` is not
      loaded from skill_db).
- [x] PK damage rate ➡️ COMBAT-62 (no `pk_mode` config knob yet).
- [x] Emperium GvG branch ➡️ COMBAT-62 (defer to FEATURE-15; the normal plant path is
      unaffected).

## Done criteria

- Bash on a Flora (MD_IGNOREMELEE) deals exactly 1; a magic skill bypasses (already). ✅
- A weapon skill on a GvG map deals `gvg_weapon_damage_rate%` ✅; an
  INF2_IGNOREGVGREDUCTION skill is unscaled ➡️ COMBAT-62.
- SC_INVINCIBLE target takes 1 from any lane. ✅ (dormant where `_sc` is null — COMBAT-59)

## Test plan

- `Combat42WeaponSkillPlantZoneTests`: weapon skill on plant → 1; weapon skill on GvG
  → 60%; INF2-ignore skill unscaled; SC_INVINCIBLE → 1; PK rate when pk_mode set.

## Notes / gotchas

- Plant and GvG are mutually exclusive (infdef returns before gvg_bg) — keep that in the
  weapon-skill path too. Reuse `BattleCalculator.ApplyPlantAndZone`.

## History

- 2026-06-02 · Added `IBattleCalculator.ApplyWeaponSkillPlantZone` (wraps the post-ratio
  weapon-skill total in a weapon-lane BattleDamage → `ApplyPlantAndZone`), wired into
  `WeaponSkillImpl.CastendDamageId` + the `SkillAttackService` funnel — so a Bash on a
  Flora deals 1 and a weapon skill in WoE is GvG-scaled. Threaded `IStatusChangeService`
  through `ApplyPlantAndZone`/`IsInfiniteDefense` for the SC_INVINCIBLE clamp (all four
  lanes pass it). Combat42WeaponSkillPlantZoneTests (5: melee plant, ranged-plant gate,
  SC_INVINCIBLE, zone scaler routing, plant-over-zone precedence). Full Map.Server.Tests
  green except the pre-existing INFRA-11 replay gate. Filed COMBAT-62 (INF2 ignore-
  reduction bypass + can-hit gate + PK rate + Emperium) — INF2 is data-blocked on the
  skill_db Inf2 loader. SC_INVINCIBLE live behavior is dormant where BattleCalculator._sc
  is null (COMBAT-59).
