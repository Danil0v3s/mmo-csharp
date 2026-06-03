# COMBAT-112 — recursive-splash victims skip ApplyWeaponSkillPlantZone (plant clamp / GvG-BG / SC_INVINCIBLE)

> **Epic:** combat · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** COMBAT-42 (the ApplyWeaponSkillPlantZone stage) · **Blocks:** none
> **Filed by:** COMBAT-91 — while wiring KO_HUUMARANKA/KO_BAKURETSU splash through
> `ComputeSkillDamage`, noticed the splash hierarchy applies per-victim damage *raw*, skipping
> the weapon-skill final stage that the single-target `WeaponSkillImpl` path applies.

## Problem

`WeaponSkillImpl.CastendDamageId` runs `dmg = ctx.Battle.ApplyWeaponSkillPlantZone(src, target, dmg,
isShortRange, skillId)` (COMBAT-42 — the plant 1-damage clamp, GvG/BG zone scaling, SC_INVINCIBLE)
before `ApplyDamage`. `RecursiveDamageSplashSkillImpl.SplashAround` (SkillImpl.cs) instead calls
`ctx.Damage.ApplyDamage(v, dmg, src)` on the raw `SplashDamage(...)` value — so **every** splash
skill's victims bypass that stage. A splash skill on a plant (MD_IGNOREMELEE) should deal 1 per
victim; in WoE every splash victim should be GvG-scaled; under SC_INVINCIBLE the caster's splash
should be modified. None of this fires today.

This is hierarchy-wide (affects all `RecursiveDamageSplashSkillImpl` skills — ArrowShower,
MagnumBreak, the KO_* arms COMBAT-91 just wired, the Acolyte/Mage/Swordman splash corpus, …), not
specific to one plugin, so it is its own ticket rather than per-skill.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs` — `RecursiveDamageSplashSkillImpl.SplashAround` applies
  `SplashDamage(...)` directly via `ctx.Damage.ApplyDamage`, with no `ApplyWeaponSkillPlantZone` call.
- `Map.Server/Combat/IBattleCalculator.cs` — `ApplyWeaponSkillPlantZone(src, target, dmg, isShortRange, skillId)`
  exists (COMBAT-42) and is used by the single-target weapon-skill path.

## rAthena reference (source of truth)

- `skill.cpp skill_castend_damage_id` → `map_foreachinrange(skill_area_sub, …)` → `skill_attack` →
  `battle_calc_attack` → `battle_calc_damage` (plant / GvG-BG / SC_INVINCIBLE) applies per splash victim.

## Scope

- [ ] In `SplashAround`, route the per-victim `SplashDamage` value through
      `ctx.Battle.ApplyWeaponSkillPlantZone(src, v, dmg, isShortRange, SkillId)` (resolve short/long
      range per skill) before `ApplyDamage`, mirroring `WeaponSkillImpl.CastendDamageId`.
- [ ] Confirm the magic-splash skills (if any extend this base) get the magic-side equivalent or are
      excluded — keep the weapon-vs-magic lane split correct.

## Done criteria

- A splash skill deals 1 per victim vs a plant (MD_IGNOREMELEE), is GvG/BG-scaled in WoE, and honors
  SC_INVINCIBLE — same as the single-target weapon-skill path.

## Test plan

- A `RecursiveDamageSplashSkillImpl` test: a plant victim takes 1; a normal victim takes the full
  ratio (regression-guards the existing nonzero path).
