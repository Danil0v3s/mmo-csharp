# COMBAT-62 — GvG gates: INF2 ignore-reduction + can-hit gate + PK rate + Emperium

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-42 · INF2 part also needs the skill_db `Inf2` loader
> **Blocks:** none
> **Filed by:** COMBAT-42 — the GvG gates beyond the weapon-skill plant/zone wiring.

## Problem

COMBAT-42 applied the weapon-skill plant 1-damage clamp + GvG/BG zone scaling +
SC_INVINCIBLE. Four GvG/PK gates remain:

1. **`INF2_IGNOREGVGREDUCTION` / `INF2_IGNOREBGREDUCTION` bypass.** A skill flagged
   ignore-reduction is NOT zone-scaled. **Data-blocked:** `SkillInf2` has no
   `IgnoreGvgReduction`/`IgnoreBgReduction` value, and `Inf2` is not loaded from the
   skill_db at all (no `SkillDbEntity.Inf2` column / `SkillDbLoader` parse) — same gap
   as COMBAT-35's `DisableLvDmg`. Needs the Inf2 loader first.
2. **`battle_can_hit_gvg/bg_target` → 0 gate** (the target can't be hit in this
   zone) — not modeled in `ZoneDamageService`.
3. **PK damage rate** (`battle_calc_pk_damage`, battle.cpp:2158) for PC↔PC when
   `pk_mode` is on — no pk_mode config knob exists yet.
4. **Emperium GvG branch** inside `battle_calc_attack_plant` — defer to / coordinate
   with FEATURE-15 (WoE) since the Emperium isn't spawnable yet.

## Current state (C#)

- `Map.Server/Combat/ZoneDamageService.cs:Scale` — GvG/BG rate only; no `skillId`,
  no INF2 bypass, no can-hit gate, no PK rate.
- `Map.Server/Skills/SkillDefinition.cs:SkillInf2` — no IgnoreGvg/Bg flags; `Inf2` is
  never loaded (always `None`).
- `Map.Server/Combat/IBattleConfigService.cs` — generic `GetValue(knob)`; no pk_mode.

## rAthena reference

- `battle.cpp:2051/2126` `battle_can_hit_bg/gvg_target` + the INF2 ignore bypass.
- `battle.cpp:2158` `battle_calc_pk_damage`.
- `battle.cpp:7104-7118` Emperium branch (`battle_can_hit_gvg_target` +
  `battle_calc_gvg_damage`).

## Scope

- [x] Added `IgnoreGvgReduction`/`IgnoreBgReduction` to `SkillInf2`; surfaced them via a
      curated Inf2 overlay in `SkillDb.LoadingFinished` (NJ_ZENYNAGE + GN_FIRE_EXPANSION_ACID —
      the only two renewal skills with the flags); threaded `skillId` + `ISkillDb.GetInf2`
      through `ApplyPlantAndZone`/`ApplyWeaponSkillPlantZone` into `ZoneDamageService.Scale` to
      bypass zone scaling.
- [x] PK damage rate for PC↔PC under the `pk_mode` knob (`battle_calc_pk_damage`) — applied in
      `ZoneDamageService` independently of the GvG/BG zone (stacks), with rAthena's default
      rates (weapon/magic/misc 60, short 80, long 70).
- [ ] `battle_can_hit_gvg/bg_target` → 0 gate. ➡️ Moved to COMBAT-80 — entirely WoE-entity-gated
      (guardian/`AI_GUILD`/`MD_SKILLIMMUNE`/Emperium/`immune_attack`); the trigger conditions
      have no representation in the entity model yet (no guardian/Emperium spawn), so a gate
      would be a dead always-true no-op until FEATURE-15.
- [ ] Emperium GvG branch. ➡️ Moved to COMBAT-80 (coordinate with FEATURE-15 — the Emperium
      isn't spawnable).

## Done criteria

- ➡️ from COMBAT-42: an `INF2_IGNOREGVGREDUCTION` weapon skill on a GvG map is unscaled ✅;
  PK rate applies when pk_mode is on ✅. A can't-hit GvG target takes 0 ➡️ COMBAT-80
  (WoE-entity-gated).

## Test plan

- INF2-ignore skill unscaled on GvG/BG; PK rate (PC↔PC, lane + short/long, off when pk_mode 0,
  off vs non-PC); PK+GvG stack. ✅ Combat62GvgPkGatesTests (8). can't-hit → 0 ➡️ COMBAT-80.

## History

- 2026-06-02 · Shipped the INF2 ignore-reduction bypass + PK damage rate. Added
  `SkillInf2.IgnoreGvgReduction`/`IgnoreBgReduction` + a curated `SkillDb.LoadingFinished`
  overlay (NJ_ZENYNAGE 526 / GN_FIRE_EXPANSION_ACID 2489); threaded `skillId` through
  `ApplyPlantAndZone`/`ApplyWeaponSkillPlantZone`/`IZoneDamageService.Scale`; added the
  `pk_mode` PC↔PC rate (`battle_calc_pk_damage`) to `ZoneDamageService`. Combat62GvgPkGatesTests
  (8); combat+skills suite 3076 green, full suite 4044 pass (1 fail = pre-existing INFRA-11
  replay gate). Filed COMBAT-80 (can-hit GvG/BG gate + Emperium branch — WoE-entity-gated,
  coordinate FEATURE-15).
