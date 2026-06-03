# SK-NPC — Npc mob-skill family (45 shells)

> **Epic:** skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** SK-ENGINE · **Unlocks:** none

## The deliverable

> The 45 `NPC_*` mob-skill shells (used by monsters/bosses) get their real rAthena behaviour so
> mobs cast them correctly.

## What this absorbs (archive)

- `_archive/todo/skills/SKILL-08` — Family: Npc (45 mob-skill shells).

## rAthena reference

- `rathena/src/map/skill.cpp` — the `NPC_*` `case` arms (mob skill effects, summons, debuffs, AoE).

## Scope

- [ ] Port each of the 45 `NPC_*` mob skills: damage/effect/summon/debuff, splash allegiance
      (SKILL-03 resolver), durations from `skill_db`.

## Done criteria

- Mobs casting these skills produce the rAthena effect; per-skill tests pass; no default-shell behaviour.

## Test plan

- Per-skill tests (mob-cast path).

## Notes

- Deferred. Uses the SKILL-03 splash allegiance resolver + SK-ENGINE.
