# SK-MISSING — The ~25 missing skills + 22 `_ATK` sub-skills

> **Epic:** skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The ~25 skills with no plugin at all get real bodies, and the 22 `_ATK` sub-skill invocations
> are verified to fire.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-06` — port the ~25 genuinely-missing skills + verify 22 `_ATK` sub-skills.

## rAthena reference

- `rathena/src/map/skill.cpp` — the `case` bodies for the missing skill ids (the archive lists them).

## Scope

- [ ] Port each missing skill's real behaviour (ratio/effect/unit).
- [ ] Verify the `_ATK` sub-skill dispatch fires for the 22 parent skills.

## Done criteria

- Each previously-missing skill produces its rAthena effect; the `_ATK` sub-skills invoke; tests pin them.

## Test plan

- Per-skill tests for the ported skills.

## Notes

- Deferred. The archive enumerates the exact id list.
