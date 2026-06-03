# COMBAT-75 — SC_KAGEMUSYA ratio bonus across the Ninja/Kagerou skill arms

> **Epic:** combat · **Status:** ✅ Done (2026-06-03) · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Blocks:** none
> **Filed by:** COMBAT-57 — the KO_JYUMONJIKIRI arm's KAGEMUSYA bonus (one of 11), out of that ticket's scope.

> **Premise correction:** the ticket said "11 arms", but at this rAthena checkout the
> `skillratio += skillratio * SC_KAGEMUSYA->val2/100` close exists on exactly **3**
> `battle_calc_attack_skill_ratio` arms — KO_JYUMONJIKIRI (battle.cpp:5644), KO_HUUMARANKA (5650),
> KO_BAKURETSU (5667). The other `getSCE(SC_KAGEMUSYA)` hits are the double-attack *rate* (4441/4445)
> and a `damagevalue` term (4257) in different functions, not the skill-ratio multiply.

## Problem

rAthena's `battle_calc_attack_skill_ratio` applies an SC_KAGEMUSYA caster bonus —
`skillratio += skillratio * sc->getSCE(SC_KAGEMUSYA)->val2 / 100` — to **11 arms** (the
Ninja / Kagerou-Oboro damage skills, e.g. KO_JYUMONJIKIRI battle.cpp:5639, KO_HUUMARANKA,
KO_SETSUDAN, KO_BAKURETSU, etc.). The C# port applies it on at most one (`SwirlingPetal`
references it), so a caster under Shadow Warrior (SC_KAGEMUSYA) does not get the ratio
multiplier on most of these skills.

## Current state (C#)

- `Map.Server/Combat/BattleCalculator.cs` / the per-skill plugins — no shared SC_KAGEMUSYA
  ratio multiplier. `SwirlingPetal` mentions it but the rest of the arm set does not apply it.
- COMBAT-57 added `CalculateSkillRatioPostDmod` (a post-RE_LVL_DMOD ratio hook) — note the
  KAGEMUSYA bonus in rAthena is applied AFTER the SC_JYUMONJIKIRI add but is a `*val2/100`
  multiply of the running skillratio (so it scales whatever ratio is present at that point).

## rAthena reference (source of truth)

- `battle.cpp` the 11 `if (sc && sc->getSCE(SC_KAGEMUSYA)) skillratio += skillratio * val2/100;`
  occurrences (grep `getSCE(SC_KAGEMUSYA)` in `battle_calc_attack_skill_ratio`).
- `status.cpp` SC_KAGEMUSYA `val2` (the Shadow Warrior ratio bonus magnitude).

## Scope — every sub-system that must be touched

- [x] Apply the `skillratio += skillratio * KAGEMUSYA.val2 / 100` multiply to each affected
      Ninja/Kagerou skill. → New `SkillImpl.CalculateSkillRatioPostDmodMultiply(ratio, …)` hook +
      shared `ApplyKagemusyaRatio(ratio, src, ctx)` helper (reads SC_KAGEMUSYA.val2, no-op without
      the SC / without ctx); `WeaponSkillImpl.ComputeSkillDamage` calls it as the final ratio step.
      Overridden on KO_JYUMONJIKIRI (`KoCrossSlash`) — the single-target arm that flows through
      `ComputeSkillDamage`. ➡️ The two **splash** arms KO_HUUMARANKA / KO_BAKURETSU are moved to
      **COMBAT-91**: their `RecursiveDamageSplashSkillImpl.SplashDamage` returns 0 (deal no damage at
      all today) and never reach `ComputeSkillDamage`, so a multiply override there would be dead
      code — COMBAT-91 wires their damage path (+ the missing `pc_checkskill` base terms) and then
      applies the multiply.
- [x] Confirm the ordering matches rAthena (after the per-skill base + any SC_JYUMONJIKIRI add). →
      the multiply runs after `CalculateSkillRatioPostDmod` (the SC_JYUMONJIKIRI add), so it scales
      the full running ratio; pinned by `Kagemusya_multiplies_after_the_jyumonjikiri_post_dmod_add`.

## Done criteria

- A caster with SC_KAGEMUSYA active deals the `×(100+val2)/100` boosted ratio on KO_JYUMONJIKIRI
  (the live single-target arm). ➡️ KO_HUUMARANKA / KO_BAKURETSU **moved to COMBAT-91** (their splash
  damage path is unwired; the multiply is dead code until COMBAT-91 lands).

## Test plan

- `Combat75KagemusyaTests`: a representative skill (e.g. KO_JYUMONJIKIRI) with vs without
  SC_KAGEMUSYA on the caster → boosted ratio by val2%.

## Notes / gotchas

- It multiplies the running skillratio (including the COMBAT-57 SC_JYUMONJIKIRI add), so apply it
  as the last ratio step. Reuse the COMBAT-57 ctx-aware / post-DMOD ratio path.

## History

- 2026-06-03 — Added the multiplicative ratio close hook `SkillImpl.CalculateSkillRatioPostDmodMultiply`
  + shared `ApplyKagemusyaRatio` helper (SC_KAGEMUSYA val2=20, status.cpp:11980), wired into
  `WeaponSkillImpl.ComputeSkillDamage` after the post-DMOD additions so it scales the full running
  ratio exactly as rAthena's `skillratio += skillratio * val2/100`. Overrode it on `KoCrossSlash`
  (KO_JYUMONJIKIRI). Corrected the "11 arms" premise to the 3 real ratio arms; the splash arms
  KO_HUUMARANKA/KO_BAKURETSU were found to deal no damage (SplashDamage→0, never hit ComputeSkillDamage)
  → filed **COMBAT-91** for their damage-path wiring + base-ratio terms + the KAGEMUSYA multiply, and
  rewrote their misleading docstrings to cite it. Combat75KagemusyaTests (3: ×120% boost, multiply-after-
  JYUMONJIKIRI-add ordering, no-SC no-op); Skills+Combat 3104 green, full suite 4124 pass (1 fail =
  pre-existing INFRA-11 `dhxj.log` replay gate, unrelated).
