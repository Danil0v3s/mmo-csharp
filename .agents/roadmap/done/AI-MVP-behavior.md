# AI-MVP — MVP bosses behave like MVPs

> **Epic:** mobai · **Status:** ✅ Done (2026-06-04) · **Size:** M · **Player-visible:** yes
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

- [x] **AI**: MVP skill-priority selection, HP-threshold announce, teleport/heal/summon behaviour.
      **Root cause found + fixed:** the entire mob-skill-cast pipeline (`MobSkillCastService` picker,
      condition registry, target resolver, mob chat, slave summon) was already built and wired into the
      AI tick — but `MobDbEntry.Skills` was **always empty** because nothing loaded `mob_skill_db`. So no
      mob (MVP or trash) ever cast a skill. Added the loader: `MobDb.LoadSnapshot` now joins
      `IMobSkillDbRepository` and attaches each mob's skill rows (in seeded/priority order) via
      `MobSkillRowMapper`, which maps the rAthena state/condition/target strings onto the pinned enums
      (`attack`→Berserk, `chase`→Rush, `randomtarget`→Random, `myhpltmaxrate`/`slavele`/`rudeattacked`/…
      conditions, cond2/emotion/chat payload). The picker then drives skill priority, the HP-threshold
      chat announce (`myhpltmaxrate` + ChatId), and summon/heal/teleport — all on the existing,
      already-tested execution path.
- [x] **Drops**: the MVP drop tier on death — already implemented in `MobDeathObserver.AwardMvp`
      (rolls `MvpDrops` with `isMvpDrop: true` + MVP exp + the world announce); verified at HEAD. The
      player-side MVP reward stays in GP-MVPFAME.

## Done criteria

- ✅ An MVP boss uses skills by priority, announces at HP thresholds, summons/teleports per its AI,
  and rolls the MVP drop tier; tests pin the behaviour. (`MobSkillLoadTests` pin the loader/mapper;
  the existing `RathenaMobSkillSweepTests`/`MobSkillCastServiceTests` pin that a loaded skill list
  fires teleport/heal/summon/powerup/chat under the right conditions; `MobDeathObserver` tests pin the
  MVP drop tier.)
- Boss wider active-radius + the floating boss-HP bar (the archived MOBAI-02's broader boss-AI items,
  not part of these done-criteria) ➡️ **AI-BOSS-ACTIVE-HP**.

## Test plan

- AI behaviour tests (skill priority, HP announce, summon) + a drop-tier test.

## Notes

- Parallel. The MVP *reward to the player* (item/exp/effect) is GP-MVPFAME; this is the boss's own AI.

## History

- 2026-06-04 — Done. The deliverable turned out to be one missing loader: every mob's `Skills` list was
  empty, so the whole (already-built) mob-skill-cast pipeline never fired. `MobDb` now loads
  `mob_skill_db` (11,634 seeded rows) into `MobDbEntry.Skills` via `MobSkillRowMapper` (state/condition/
  target string → enum), which makes MVPs cast by priority, announce on HP thresholds, and summon/heal/
  teleport. The MVP drop tier was already live in `MobDeathObserver`. 16 loader/mapper tests; full
  Map.Server.Tests 4542 pass (1 = standing replay-fixture). Filed AI-BOSS-ACTIVE-HP (boss active-radius
  + boss-HP bar, the archived MOBAI-02's residual boss-AI items).
