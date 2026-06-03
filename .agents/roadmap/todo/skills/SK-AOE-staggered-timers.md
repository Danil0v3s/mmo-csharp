# SK-AOE — Position-staggered AoE timers

> **Epic:** skills · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> Position-targeted AoE skills (Meteor Storm, comet/train skills) land their cells on the correct
> staggered timers instead of all at once.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-02` — position-targeted staggered AoE timers (`skill_addtimerskill(x,y)`).

## rAthena reference

- `rathena/src/map/skill.cpp` — `skill_addtimerskill` with (x,y) target; the per-cell delay
  ladder for Meteor Storm / Storm Gust trains.

## Scope

- [ ] Position-targeted `skill_addtimerskill(x,y)` path so AoE cells fire on their staggered delays.

## Done criteria

- Meteor Storm drops its meteors on the rAthena cadence (staggered), each at its cell; the test pins the timing.

## Test plan

- Extend the archived SKILL-02 test (cell timing ladder).

## Notes

- Deferred. Builds on SK-ENGINE's context threading.
