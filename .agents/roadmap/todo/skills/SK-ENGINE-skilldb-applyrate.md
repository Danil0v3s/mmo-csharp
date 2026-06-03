# SK-ENGINE — Plugins read skill_db durations/vals + apply-rate everywhere

> **Epic:** skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** SK-TAEKWON/NPC/NINJA/GUNSLINGER/HOMUN/CLASSIC, CB-WEAPON

## The deliverable

> Skill plugins read their SC durations + values from `skill_db` (not hardcoded), every SC proc
> rolls through the apply-rate engine (not `Random.Shared`), and the `SkillBehaviorContext` is
> threaded through the attack funnel so ctx-aware ratios work. This is the **foundation the
> family tickets build on.**

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-04` — read SC durations/vals from `skill_db` (`GetTime2/3`) in plugins.
- `_archive/todo/skills/SKILL-14` — bulk-migrate the remaining ~163 plugin SC-proc rolls onto the apply-rate engine.
- `_archive/todo/skills/SKILL-15` — `ScDefTable` depth: bespoke-formula SCs + min_rate/min_duration + resist-buff adds.
- `_archive/todo/skills/SKILL-17` — thread `SkillBehaviorContext` through the `SkillAttack` funnel for ctx-aware ratios.

## rAthena reference

- `rathena/src/map/skill.cpp` — `skill_get_time2`/`skill_get_time`/`skill_get_*` reads;
  `status_get_sc_def` apply-rate (the archived SKILL-01 built the engine).

## Scope

- [ ] Add `ISkillDb` to the plugin context; replace hardcoded SC durations/Vals with `GetTime2/3` reads.
- [ ] Migrate the remaining ~163 plugin SC-proc rolls onto the rate-aware `Start(rate,…)` engine.
- [ ] `ScDefTable` depth (bespoke formula SCs + min_rate/min_duration + resist adds).
- [ ] Thread `SkillBehaviorContext` through the `SkillAttackService` funnel (unblocks ctx-aware
      ratios for splash/plain plugins — used by CB-WEAPON).

## Done criteria

- Plugins read durations from `skill_db`; no plugin rolls a raw `Random.Shared` for an SC proc;
  the ctx-aware ratio funnel reaches the splash/plain plugins; the skill suite is green.

## Test plan

- Extend the archived SKILL-04/14/15/17 tests; the apply-rate migration guard passes.

## Notes

- Land this FIRST in the skills phase — the family tickets + CB-WEAPON depend on it.
