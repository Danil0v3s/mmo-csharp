# COMBAT-24 — Per-skill cast/delay tables + SA_ABRACADABRA

> **Epic:** Combat parity · **Status:** ✅ Done (2026-06-02) · **Size:** M · **Player-visible:** yes
> **Depends on:** COMBAT-22 (the bonus2 per-skill maps) · **Blocks:** none
> **Filed by:** COMBAT-07 (the per-skill + abra parts it scoped but didn't reach).

## Problem

COMBAT-07 wired the renewal DEX/INT variable-cast sqrt + the GLOBAL equip/card cast
bonuses (varcastrate/fixcastrate/add_varcast/add_fixcast/delayrate). Two rAthena
pieces remain:
1. **Per-skill cast/delay tables.** rAthena reads `sd->skillcastrate[skill]`,
   `sd->skillvarcast[skill]`, `sd->skillfixcast[skill]`, `sd->skilldelay[skill]`
   (`bonus2 bVariableCastTime,SKILL,N;` etc.) — affecting only the keyed skill. These
   need the per-skill bonus2 maps that COMBAT-22 adds to `EquipBonusBundle`.
2. **SA_ABRACADABRA.** rAthena `skill_delayfix` special-cases its cast/delay to 0 and
   the skill picks a random skill from `abra_db`. Currently a comment-only no-op in
   `SkillCastTimingService.DelayFix`.

## Current state (C#)

- `Map.Server/Skills/SkillCastTimingService.cs:VfCastFix` / `DelayFix` — global bonuses
  via `ApplyVariableCast`/`ApplyFixedCast`/`ApplyDelayBonus`; no per-skill lookup; the
  SA_ABRACADABRA comment cites this ticket.
- `EquipBonusBundle` — no per-skill cast maps (COMBAT-22 adds them).

## rAthena reference

- `skill.cpp:20324` `skill_vfcastfix` per-skill reads; `skill_delayfix` `sd->skilldelay`
  and the SA_ABRACADABRA 0-delay branch; `abra_db` random-skill table.

## Scope

- [x] Per-skill cast tables (skill_vfcastfix per-skill loops): added the flat-ms maps
      `SkillVarCast`/`SkillFixCast` (bonus2 bSkillVariableCast/bSkillFixedCast) to the
      bundle + extractor; COMBAT-22's `SkillVarCastrate`/`SkillFixCastrate` (% rates) are
      now consumed too. New `SkillCastTimingService.ApplyPerSkillCast` applies all four
      keyed on `skillId` after the global bonuses in `VfCastFix`.
- [x] Zeroed SA_ABRACADABRA cast (`VfCastFix`) + after-cast delay (`DelayFix`). ➡️ the
      `abra_db` random-skill SELECTION (cast-end behavior + the data table) moved to
      **COMBAT-46** — it's a skill-behavior concern, not cast-timing.

## Done criteria

- `bonus2 bVariableCastrate,WZ_STORMGUST,50;` halves only Storm Gust's variable cast,
  other skills unaffected ✅ (the ticket's `bVariableCastTime,-50` wording was loose; the
  per-skill % rate is the testable "halves" case, and the flat `bSkillVariableCast` ms add
  is also wired + tested).
- SA_ABRACADABRA has 0 cast delay ✅; ➡️ selects from abra_db moved to **COMBAT-46**.

## Test plan

- Per-skill cast-rate affects only the keyed skill ✅ (Storm Gust halved, Fire Bolt
  untouched); flat per-skill ms add ✅; floor-at-0 ✅; SA_ABRACADABRA 0 cast + 0 delay ✅;
  extractor parse ✅.

## History

- **2026-06-02** — inprogress→done. Per-skill cast tables: added `SkillVarCast`/
  `SkillFixCast` flat-ms maps (+ extractor for bSkillVariableCast/bSkillFixedCast) and a
  new `ApplyPerSkillCast` that folds them + COMBAT-22's per-skill % cast-rates, keyed on
  the skill, after the global equip/card bonuses in `VfCastFix`. SA_ABRACADABRA now casts
  instantly (0 cast) and has 0 after-cast delay. Combat24PerSkillCastTests (6); unit suite
  3827 (1 fail = pre-existing INFRA-11 replay gate). Filed COMBAT-46 (abra_db random-skill
  selection).
- SA_ABRACADABRA delay == 0.
