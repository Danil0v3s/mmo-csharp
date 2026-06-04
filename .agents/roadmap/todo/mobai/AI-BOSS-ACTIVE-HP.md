# AI-BOSS-ACTIVE-HP — boss mobs stay active at range + show their HP bar

> **Epic:** mobai · **Status:** ❌ Not started · **Size:** S · **Player-visible:** yes
> **Depends on:** none · **Unlocks:** none

## The deliverable

> Boss-type mobs (MD_MVP / MD_STATUSIMMUNE) **keep their hard AI (skill use + chase) at a wider
> active radius** (rAthena <c>battle_config.boss_active_time</c>) instead of the plain view range, and
> the client shows the **floating boss HP bar** (rAthena <c>monster_hp_bar</c> / <c>clif_summon_hp_bar</c>).

## Why it matters / current state

AI-MVP made MVP bosses use their full skill kit (the mob_skill_db loader) + roll the MVP drop tier.
Two boss-flavour items from the original (archived) MOBAI-02 scope remain, and are **not** part of
AI-MVP's done-criteria:

1. **Wider active radius.** `Map.Server/Mob/MobAiService.cs:HasAnyPcInView` uses the plain
   `ChaseRange` for every mob; its own docstring flags that boss mobs should use
   `battle_config.boss_active_time` so a far-away boss stays in hard AI longer (rAthena keeps bosses
   "active" at a larger radius than trash mobs). Today a boss out of plain view range drops to lazy AI
   and stops using skills/chasing.
2. **Boss HP bar.** No `ZC` boss-HP / `monster_hp_bar` packet exists under `Core.Server/Packets/Out/`,
   so the client never shows a boss's remaining HP.

## rAthena reference

- `rathena/src/map/mob.cpp` — `mob_ai_sub_hard` active-radius gate; `battle_config.boss_active_time`.
- `rathena/src/map/clif.cpp` — `clif_summon_hp_bar` / `clif_monster_hp_bar` (the boss HP-bar emit).

## Scope

- [ ] **AI**: a wider active radius for boss mobs (MD_MVP / MD_STATUSIMMUNE) in `HasAnyPcInView` (and the
      lazy/hard split), gated on the boss mode bits.
- [ ] **ZC**: the boss-HP-bar packet + emit on the boss taking damage / entering a PC's view.

## Done criteria

- A boss stays in hard AI (uses skills, chases) at a wider radius than a normal mob; tests pin the
  radius delta.
- The client shows the floating boss HP bar that updates as the boss takes damage.

## Test plan

- AI test: boss vs normal mob active-radius (boss stays hard where a normal mob goes lazy).
- HP-bar emit test.

## Notes

- Filed by AI-MVP (the mob-skill loader). These are the residual boss-AI items from the archived
  MOBAI-02 that AI-MVP's done-criteria did not cover.
