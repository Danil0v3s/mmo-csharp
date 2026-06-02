# COMBAT-62 — GvG gates: INF2 ignore-reduction + can-hit gate + PK rate + Emperium

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Surface `Inf2` through `SkillDbEntity` + `SkillDbLoader` (or a curated set), add
      `IgnoreGvgReduction`/`IgnoreBgReduction` to `SkillInf2`, and thread `skillId` +
      `ISkillDb.GetInf2` into `ZoneDamageService.Scale` to bypass scaling.
- [ ] Add the `battle_can_hit_gvg/bg_target` → 0 gate.
- [ ] PK damage rate for PC↔PC under a `pk_mode` config knob.
- [ ] Emperium GvG branch (coordinate with FEATURE-15).

## Done criteria

- ➡️ from COMBAT-42: an `INF2_IGNOREGVGREDUCTION` weapon skill on a GvG map is unscaled;
  a can't-hit GvG target takes 0; PK rate applies when pk_mode is on.

## Test plan

- INF2-ignore skill unscaled on GvG; can't-hit → 0; PK rate when pk_mode set.
