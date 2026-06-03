# COMBAT-91 — KO_HUUMARANKA / KO_BAKURETSU splash damage path + missing base-ratio terms

> **Epic:** combat · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] Give `SwirlingPetal` / `KunaiExplosion` a real per-victim damage: override `SplashDamage` to
      run the weapon swing × the per-skill ratio (route through a shared ratio computation that also
      applies `RE_LVL_DMOD`, the post-dmod adds, and the SC_KAGEMUSYA multiply), OR re-base them on a
      hierarchy that exposes `ComputeSkillDamage`. Match `RecursiveDamageSplashSkillImpl` peers.
- [ ] Add the `NJ_HUUMA * 100` term (KO_HUUMARANKA) and the real `pc_checkskill(NJ_TOBIDOUGU)`
      factor + post-dmod `10 * job_level` (KO_BAKURETSU) via `pc_checkskill`/job-level reads.
- [ ] Apply the SC_KAGEMUSYA multiply (`ApplyKagemusyaRatio`) as the final ratio step on both arms,
      after the base + dmod + post-dmod adds (mirror KoCrossSlash).

## Done criteria

- KO_HUUMARANKA and KO_BAKURETSU deal nonzero splash damage equal to rAthena's ratio at
  representative levels (partner-skill terms included), and a caster under SC_KAGEMUSYA gets the
  `×(100+20)/100` boosted ratio on both.

## Test plan

- `Combat91KoSplashTests`: each skill's splash victim takes `swing × ratio` (with the partner-skill
  term) and the KAGEMUSYA-on vs -off ratio differs by val2%.

## Notes / gotchas

- COMBAT-75 already shipped the `CalculateSkillRatioPostDmodMultiply` hook + `ApplyKagemusyaRatio`
  helper on `SkillImpl`; reuse them. The blocker here is that the splash hierarchy doesn't compute a
  ratio-scaled damage at all — fix that first, then the KAGEMUSYA multiply is a one-line override.
- `pc_checkskill` partner-skill reads are PlayerEntity-only (non-player → the rAthena `: 1` / `: 0`
  fallbacks).
