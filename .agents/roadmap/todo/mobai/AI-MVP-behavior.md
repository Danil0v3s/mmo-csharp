# AI-MVP — MVP bosses behave like MVPs

> **Epic:** mobai · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> MVP bosses **use their skill priority, announce on HP thresholds, teleport/heal/summon per the
> MVP AI, and follow the MVP drop tier** — visible in-game.

## What this absorbs (archive)

- `_archive/todo/mobai/MOBAI-02` — MVP behavior (skill priority, hp announce, drop tier).

## rAthena reference

- `rathena/src/map/mob.cpp` — `mob_ai_sub_hard` MVP branches: `MD_MVP`, skill use priority,
  `mob_class_change`/teleport/heal/summon-slaves, the boss HP-announce, the MVP drop tier roll.

## Scope

- [ ] **AI**: MVP skill-priority selection, HP-threshold announce, teleport/heal/summon behaviour.
- [ ] **Drops**: the MVP drop tier on death (separate from the normal-drop + the MVP-reward in
      GP-MVPFAME).

## Done criteria

- An MVP boss uses skills by priority, announces at HP thresholds, summons/teleports per its AI,
  and rolls the MVP drop tier; tests pin the behaviour.

## Test plan

- AI behaviour tests (skill priority, HP announce, summon) + a drop-tier test.

## Notes

- Parallel. The MVP *reward to the player* (item/exp/effect) is GP-MVPFAME; this is the boss's own AI.
