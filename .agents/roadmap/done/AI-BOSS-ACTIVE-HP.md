# AI-BOSS-ACTIVE-HP — boss mobs stay active at range + show their HP bar

> **Epic:** mobai · **Status:** ✅ Done (2026-06-04) · **Size:** S · **Player-visible:** yes
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

- [x] **AI**: the boss/mob active-window (rAthena's actual mechanism is **time-based**, not a wider
      radius — `mob_active_time` / `boss_active_time`, battle.cpp default 5000 ms). `MobAiService.Tick`
      now records `last_pcneartime` when a PC is in view, and the lazy/hard gate (`ShouldRunLazy`) keeps
      a mob on the HARD path for `mob_active_time` (`boss_active_time` for `MD_STATUSIMMUNE` bosses) ms
      after the last PC leaves view — so a boss doesn't instantly drop to lazy when a player steps just
      out of range.
- [x] **ZC**: boss/mob HP bar — new `ZC_HP_INFO` (0x0977, `<id>.L <hp>.L <maxHP>.L`), emitted from
      `DamageService` (rAthena `clif_monster_hp_bar`, gated by `monster_hp_bars_info` default-on) to
      nearby players when a damaged mob's HP is below max (skipped on the killing blow).

## Done criteria

- ✅ A boss stays in hard AI for `boss_active_time` ms after the last PC leaves view (vs a mob with no
  recent PC contact, which goes lazy at once); tests pin the window boundary.
  (`MobAiServiceTests.Mob_stays_on_the_hard_path…` / `…goes_lazy_at_once` / `Boss_mob_uses_the_boss_active_window`.)
- ✅ The client shows the floating mob HP bar that updates as the mob takes damage
  (`DamageServiceTests.ApplyDamage_BroadcastsMonsterHpBar_WhenBelowMax`, and not on the killing blow).

## History

- 2026-06-04 — Done. rAthena's boss-active behaviour is the time-based `mob_active_time`/`boss_active_time`
  lazy-AI extension (not a wider radius); ported via `last_pcneartime` + the `ShouldRunLazy` gate in
  `MobAiService`. The mob HP bar is the new `ZC_HP_INFO` (0x0977) emitted from `DamageService` on damage.
  5 tests (3 active-window + 2 HP-bar); full Map.Server.Tests 4547 pass (1 = standing replay-fixture).

## Test plan

- AI test: boss vs normal mob active-radius (boss stays hard where a normal mob goes lazy).
- HP-bar emit test.

## Notes

- Filed by AI-MVP (the mob-skill loader). These are the residual boss-AI items from the archived
  MOBAI-02 that AI-MVP's done-criteria did not cover.
