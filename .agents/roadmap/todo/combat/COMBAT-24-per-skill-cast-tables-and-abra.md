# COMBAT-24 — Per-skill cast/delay tables + SA_ABRACADABRA

> **Epic:** Combat parity · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
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

- [ ] After COMBAT-22 lands `SkillVarCast`/`SkillFixCast`/`SkillCastRate`/`SkillDelay`
      maps (skillId→value), read them keyed on `skillId` in `ApplyVariableCast`/
      `ApplyFixedCast`/`DelayFix` (thread the per-skill values in).
- [ ] Wire `abra_db` (random-skill table) and zero SA_ABRACADABRA cast/delay.

## Done criteria

- `bonus2 bVariableCastTime,WZ_STORMGUST,-50;` halves only Storm Gust's variable cast;
  other skills unaffected.
- SA_ABRACADABRA has 0 cast delay and selects from abra_db.

## Test plan

- Per-skill cast-rate affects only the keyed skill (helper takes the per-skill value).
- SA_ABRACADABRA delay == 0.
