# SK-HOMUN — Homunculus / Summoner / Novice skill families

> **Epic:** skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SK-ENGINE, GP-HOMUN (homun must exist to cast) · **Unlocks:** none

## The deliverable

> The Homunculus (`HVAN_*`/`HLIF_*`/`HAMI_*`/`MH_*`), Summoner (`SU_*`), and Expanded-Novice
> skill shells get their real ratios/effects.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-11` — Family: Homunculus / Summoner / Novice shells.

## rAthena reference

- `rathena/src/map/skill.cpp` — the `HVAN_/HLIF_/HAMI_/MH_/SU_/NV_/SU_` `case` arms.

## Scope

- [ ] Port each homun/summoner/novice skill: ratio/constant, element, splash, SC procs, the
      homun-cast gate.

## Done criteria

- Each skill in these families produces the rAthena effect; per-skill tests pass.

## Test plan

- Per-skill tests (homun-cast + summoner-cast paths).

## Notes

- Deferred. Homun skills need a live homun (GP-HOMUN) to cast in production; the bodies can be
  ported + unit-tested independently.
