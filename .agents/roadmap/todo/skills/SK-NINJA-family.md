# SK-NINJA — Ninja skill family (7 splash shells)

> **Epic:** skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The 7 Ninja splash skills get their real ratios/elements/effects (Throw, Flip Tatami, Cicada,
> the elemental scrolls).

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-09` — Family: Ninja (7 splash shells).

## rAthena reference

- `rathena/src/map/skill.cpp` — the `NJ_*` `case` arms (ratios, elements, ground units).

## Scope

- [ ] Port each Ninja skill: ratio/constant, element, splash, ground-unit/SC effects.

## Done criteria

- Each Ninja skill computes the rAthena ratio + element + effect; per-skill tests pass.

## Test plan

- Per-skill tests.

## Notes

- Deferred. Depends on SK-ENGINE.
