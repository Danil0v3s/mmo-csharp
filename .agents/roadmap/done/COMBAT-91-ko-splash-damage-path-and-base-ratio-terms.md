# COMBAT-91 — KO_HUUMARANKA / KO_BAKURETSU splash damage path + missing base-ratio terms

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-75 (the SC_KAGEMUSYA ratio multiply hook) · **Blocks:** none
> **Filed by:** COMBAT-75 — while wiring the SC_KAGEMUSYA ratio multiply, the two Kagerou
> *splash* arms turned out to compute **no damage at all** (their ratio path is dead), so the
> KAGEMUSYA multiply (and their base ratio) cannot take effect there yet.

## Problem

`SwirlingPetal` (KO_HUUMARANKA) and `KunaiExplosion` (KO_BAKURETSU) extend
`RecursiveDamageSplashSkillImpl`, whose `SplashDamage(...)` **returns 0 by default** and is
**not overridden** by either plugin. Their `CastendDamageId` → `SplashAround` therefore applies
zero damage to every victim (`if (dmg > 0)` is never true). Because `ComputeSkillDamage` (the
ratio authority, where COMBAT-75 placed the `CalculateSkillRatioPostDmodMultiply` /
SC_KAGEMUSYA close) lives only on `WeaponSkillImpl`, these two skills never reach it — so their
`CalculateSkillRatio` override is dead and the KAGEMUSYA bonus can't apply. On top of that, their
**base ratio is incomplete** vs rAthena:

- KO_HUUMARANKA omits the `pc_checkskill(sd, NJ_HUUMA) * 100` partner-skill term.
- KO_BAKURETSU hardcodes `pc_checkskill(sd, NJ_TOBIDOUGU)` as `1` (always), and adds the
  `10 * job_level` as a flat `+10` **inside** `CalculateSkillRatio` (so it is wrongly scaled by
  `RE_LVL_DMOD(120)`); rAthena adds `10 * job_level` AFTER the macro.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillImpl.cs:336` — `RecursiveDamageSplashSkillImpl.SplashDamage`
  returns `0`; `SplashAround` skips zero-damage victims.
- `Map.Server/Skills/Behaviors/Ninja/SwirlingPetal.cs` — `CalculateSkillRatio` = `-100 + 150*lv + STR`
  (missing `NJ_HUUMA*100`); no `SplashDamage`; no KAGEMUSYA multiply.
- `Map.Server/Skills/Behaviors/Ninja/KunaiExplosion.cs` — `CalculateSkillRatio` hardcodes the
  TOBIDOUGU factor as `1`, folds `+10` into the base (pre-dmod); no `SplashDamage`; no KAGEMUSYA multiply.
- `Map.Server/Skills/Behaviors/Ninja/KoCrossSlash.cs` — DONE in COMBAT-75 (the single-target arm
  that flows through `ComputeSkillDamage`; KAGEMUSYA multiply live + tested).

## rAthena reference (source of truth)

- `battle.cpp:5647` KO_HUUMARANKA: `skillratio += -100 + 150*skill_lv + sstatus->str + (sd ? pc_checkskill(sd,NJ_HUUMA)*100 : 0); RE_LVL_DMOD(100); if (sc && SC_KAGEMUSYA) skillratio += skillratio*val2/100;`
- `battle.cpp:5663` KO_BAKURETSU: `skillratio += -100 + (sd ? pc_checkskill(sd,NJ_TOBIDOUGU) : 1)*(50 + dex/4)*skill_lv*4/10; RE_LVL_DMOD(120); skillratio += 10*(sd ? job_level : 1); if (sc && SC_KAGEMUSYA) skillratio += skillratio*val2/100;`
- Both are recursive splash (`skill_castend_damage_id` → `map_foreachinrange` → `skill_attack`) —
  each splash victim takes the full weapon-attack × ratio (not 0).

## Scope — every sub-system that must be touched

- [x] Moved `ComputeSkillDamage` from `WeaponSkillImpl` up to `SkillImpl` (transparent — every
      `WeaponSkillImpl` still inherits it) so `RecursiveDamageSplashSkillImpl` subclasses can reach
      the full ratio pipeline. `SwirlingPetal`/`KunaiExplosion` now override `SplashDamage` to run a
      skill-aware `CalcWeaponAttack(SkillId)` swing × `ComputeSkillDamage` (ratio → `RE_LVL_DMOD` →
      post-dmod → SC_KAGEMUSYA multiply).
- [x] Added the `NJ_HUUMA*100` term (KO_HUUMARANKA, PC-only, `: 0` fallback) and the real
      `pc_checkskill(NJ_TOBIDOUGU)` factor (PC-only, `: 1` fallback) + post-dmod `10*job_level`
      (KO_BAKURETSU, via `CalculateSkillRatioPostDmod` so it is NOT scaled by `RE_LVL_DMOD(120)`).
      Added `SkillIds.NJ_TOBIDOUGU = 522` and `ReLvlDivisor` 120 (BAKURETSU) / 100 (HUUMARANKA).
- [x] Applied the SC_KAGEMUSYA multiply via `CalculateSkillRatioPostDmodMultiply` →
      `ApplyKagemusyaRatio` on both arms (mirror KoCrossSlash), as the final ratio step.
- [x] ➡️ The per-victim weapon-skill **final stage** (`ApplyWeaponSkillPlantZone`: plant 1-dmg
      clamp / GvG-BG zone / SC_INVINCIBLE) that the single-target path applies is NOT applied by the
      splash hierarchy — hierarchy-wide (all splash skills), pre-existing, **COMBAT-112**.

## Done criteria

- ✅ KO_HUUMARANKA and KO_BAKURETSU deal nonzero splash damage equal to rAthena's ratio at
  representative levels (partner-skill terms included): HUUMARANKA lv5 = 800% (no NJ_HUUMA) / 1800%
  (NJ_HUUMA 10); BAKURETSU lv5 = 1320% (NJ_TOBIDOUGU 5 + post-dmod 10*70), 700% with factor 0; and a
  caster under SC_KAGEMUSYA gets the `×(100+20)/100` boosted ratio on both. ➡️ The plant/GvG
  per-victim stage is COMBAT-112 (separate from the ratio — the ratio is rAthena-exact here).

## Test plan

- ✅ `Combat91KoSplashTests` (7 cases): each skill's splash victim takes `swing × ratio` (with the
  partner-skill term), the TOBIDOUGU factor is read (not hardcoded 1), the `+10*job_level` is
  post-dmod (verified at lv150 — unscaled by the macro), and KAGEMUSYA-on vs -off differs by val2%.

## Notes / gotchas

- COMBAT-75 already shipped the `CalculateSkillRatioPostDmodMultiply` hook + `ApplyKagemusyaRatio`
  helper on `SkillImpl`; reused them. The blocker was that the splash hierarchy didn't compute a
  ratio-scaled damage at all — fixed by exposing `ComputeSkillDamage` on `SkillImpl`.
- `pc_checkskill` partner-skill reads are PlayerEntity-only (non-player → the rAthena `: 1` / `: 0`
  fallbacks).

## History

- 2026-06-03 — Hoisted `ComputeSkillDamage` to `SkillImpl` so recursive-splash skills can reuse the
  ratio pipeline; rewrote `SwirlingPetal` (KO_HUUMARANKA) and `KunaiExplosion` (KO_BAKURETSU) to
  override `SplashDamage` through it — each splash victim now takes the rAthena-exact `swing × ratio`
  (NJ_HUUMA*100 / real pc_checkskill(NJ_TOBIDOUGU) factor / post-dmod `+10*job_level` / SC_KAGEMUSYA
  ×(100+val2)/100) instead of 0. Added `SkillIds.NJ_TOBIDOUGU = 522`. `Combat91KoSplashTests` (7,
  green); full Map.Server.Tests 4227 pass (1 fail = pre-existing INFRA-11 replay-fixture boot). Filed
  COMBAT-112 for the splash-hierarchy plant/GvG/SC_INVINCIBLE per-victim stage (pre-existing, all
  splash skills).
