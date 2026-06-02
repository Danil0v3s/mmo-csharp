# COMBAT-42 — Weapon-skill plant/zone + GvG gates (Emperium / INF2-ignore / PK)

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Apply `ApplyPlantAndZone` (isSkill=true) at the weapon-skill final point —
      `ComputeSkillDamage` result and the `SkillAttackService` weapon funnel — passing
      the skill's range type (BF_SHORT/LONG) and skillId.
- [ ] Add the `INF2_IGNOREGVGREDUCTION`/`INF2_IGNOREBGREDUCTION` bypass + the
      `battle_can_hit_gvg/bg_target` can't-hit → 0 gate to `ZoneDamageService.Scale`
      (thread `skillId` + `ISkillDb.GetInf2`).
- [ ] SC_INVINCIBLE branch in `IsInfiniteDefense` (thread the status service).
- [ ] PK damage rate (`battle_calc_pk_damage`) for PC↔PC when `pk_mode` is on.
- [ ] Emperium GvG branch (defer to / coordinate with FEATURE-15 WoE if Emperium
      isn't spawnable yet — at minimum gate it so a normal plant path is unaffected).

## Done criteria

- Bash on a Flora (MD_IGNOREMELEE) deals exactly 1; a magic skill bypasses (already).
- A weapon skill on a GvG map deals `gvg_weapon_damage_rate%`; an INF2_IGNOREGVGREDUCTION
  skill is unscaled.
- SC_INVINCIBLE target takes 1 from any lane.

## Test plan

- `Combat42WeaponSkillPlantZoneTests`: weapon skill on plant → 1; weapon skill on GvG
  → 60%; INF2-ignore skill unscaled; SC_INVINCIBLE → 1; PK rate when pk_mode set.

## Notes / gotchas

- Plant and GvG are mutually exclusive (infdef returns before gvg_bg) — keep that in the
  weapon-skill path too. Reuse `BattleCalculator.ApplyPlantAndZone`.
