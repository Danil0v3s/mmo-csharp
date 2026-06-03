# SK-TAEKWON — Taekwon skill family (37 shells)

> **Epic:** skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The 37 Taekwon/TaeKwon-Kid/Soul-Linker skills that are bare shells (default ratio 100 / 2-cell
> splash) get their real rAthena ratios, effects, and SC procs.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-07` — Family: Taekwon (37 shells).

## rAthena reference

- `rathena/src/map/skill.cpp` — the `TK_*` / `SL_*` `case` arms (ratios, kicks, stances, links).

## Scope

- [ ] Port each of the 37 Taekwon-family skills: ratio/constant, splash radius, SC procs,
      stance/ranker gates — read from `skill_db` (SK-ENGINE) where applicable.

## Done criteria

- Each Taekwon-family skill computes the rAthena ratio + effect at the cited levels; no shell
  left at default 100/2-cell; per-skill tests pass.

## Test plan

- Per-skill ratio/effect tests for the family.

## Notes

- Deferred. Depends on SK-ENGINE for skill_db duration/val reads + apply-rate.
