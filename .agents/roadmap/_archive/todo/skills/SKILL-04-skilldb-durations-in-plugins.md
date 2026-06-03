# SKILL-04 — Plugins must read skill_db durations (GetTime2/GetTime3) instead of hardcoding ms / Val

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SKILL-12

## Problem

Bodied SC-applying plugins hardcode their status durations and `val2`/`val3`/`val4`
constants as C# literals (e.g. `durationMs: 3000`, `val1: skillLevel`) instead of
reading them from the skill_db via `skill_get_time` / `skill_get_time2` /
`skill_get_time3`. rAthena's per-level duration columns live in
`db/re/skill_db.yml` and are read at cast time; hardcoding them means:

- the duration is wrong at every level except the one the literal was copied from,
- a server admin editing `skill_db.yml` (the supported tuning path) has no effect,
- `time2`/`time3` (used for the *secondary* timer — DoT tick interval, after-effect
  window, the "Val2/Val3/Val4" that rAthena packs from the time columns) are simply
  invented.

The deeper blocker: **`SkillBehaviorContext` does not even expose `ISkillDb`**, so a
plugin *cannot* call `GetTime2`/`GetTime3` even if it wanted to. `SkillDb` already
implements `GetTime`/`GetTime2`/`GetTime3` (reading `StatusDurationMs`/`Time2Ms`/
`Time3Ms`), and `SkillCastService` holds `_db`, but it never threads it into the
context. Every plugin is flying blind on durations.

## Current state (C#)

- `Map.Server/Skills/Behaviors/SkillBehaviorContext.cs:73` — the `SkillBehaviorContext` record's parameter list (39 services) has **no** `ISkillDb`. Plugins get `Entities`, `Damage`, `Battle`, `Sc`, … but never the skill catalog.
- `Map.Server/Skills/SkillDb.cs:354-356` — `GetTime`/`GetTime2`/`GetTime3` already exist and read `StatusDurationMs` / `Time2Ms` / `Time3Ms` per level with the safe clamp. They're just unreachable from plugins.
- `Map.Server/Skills/SkillCastService.cs:126` — `_db` is held by the cast service; `:391` / `:423` construct `SkillBehaviorContext` but omit `_db`.
- `Map.Server/Skills/Behaviors/Mage/MeteorStorm.cs` `ApplyAdditionalEffects` — `durationMs: 3000` literal for the stun. Should be `ctx.SkillDb.GetTime2(SkillId, skillLevel)` (or `GetTime`, per the rAthena arm).
- Pervasive across `Behaviors/` — grep for `durationMs:` literals and `val2:`/`val3:`/`val4:` integer literals in SC `Start` calls; the wave-98 families (Mage/Archer/Thief/Swordman/Merchant/Acolyte) plus Taekwon/Npc/Ninja shells all hardcode.

## rAthena reference (source of truth)

- `rathena/src/map/skill.cpp` per-skill arms — SC applications call `sc_start4(..., skill_get_time(skill_id, skill_lv))` or `skill_get_time2(...)` for the duration, never a literal. The `val1..val4` come from `skill_lv`, stat reads, and occasionally `skill_get_time2/3` repurposed as the secondary window.
- `rathena/src/map/skill.cpp` `skill_get_time` / `skill_get_time2` / `skill_get_time3` — read the `Duration1` / `Duration2` columns (and the derived third) from the skill_db row. `db/re/skill_db.yml` carries `Duration1:` (→ time/time2) and `Duration2:` (→ time3) per level.
- Example (WZ_METEOR): the stun duration is the skill_db `Duration1`/`Duration2` column for the meteor arm, not a flat 3000 ms.
- Monolithic-switch caveat: durations are *data* (`skill_db.yml`), read by `skill_get_time*` from `skill.cpp`. The C# `SkillDb` is the right home; the plugins must call through it.

## Scope — every sub-system that must be touched

- [ ] **Add `ISkillDb` to `SkillBehaviorContext`** — add `Map.Server.Skills.ISkillDb? SkillDb = null` as a new (optional, defaulted) record parameter on `SkillBehaviorContext`. Document it like the other P0 helpers ("rAthena `skill_get_*` accessor bundle — durations, splash, num, element").
- [ ] **Thread `_db` into both context constructions** — `SkillCastService.ResolveSkill` (`:423`) and `ResolveSkillAt` (`:391`) pass `_db` into the new `SkillDb` slot. (Optional ctor param means existing test call sites that build a context by hand still compile.)
- [ ] **Confirm `Time2Ms` / `Time3Ms` are populated** — `SkillDbLoader.FromEntity` must map the skill_db `Duration1`/`Duration2` columns into `StatusDurationMs` / `Time2Ms` / `Time3Ms`. Verify the loader fills them; if a column is unmapped, map it (this is the data backing for the accessors). The hand-built fallback set in `SkillDb.LoadFallback` should keep its literals for the starter skills (those are the seed, not the bug).
- [ ] **Migrate hardcoded durations** — replace `durationMs: <literal>` in SC `Start` calls across `Behaviors/` with `ctx.SkillDb?.GetTime(SkillId, skillLevel)` or `GetTime2`/`GetTime3` per the rAthena arm. Each call site's docstring cites which `skill_get_time*` rAthena uses. Where `ctx.SkillDb` is null (unit-test path with no catalog), fall back to the prior literal so tests that don't wire a db still pass — but the literal is now the *fallback*, not the source of truth.
- [ ] **Migrate invented Val2/Val3/Val4** — where a plugin hardcodes `val2`/`val3`/`val4` that rAthena derives from `skill_get_time2/3` or a stat read, replace with the correct read. Where a val is genuinely `skill_lv` (common), leave it.
- [ ] **No new packets / IPC.** Data may need a `skill_db.yml` → SQL re-seed if `Duration2` was never imported — note that as a data task, not code.

## Done criteria

- `SkillBehaviorContext.SkillDb` is non-null on the live cast path (both `ResolveSkill` and `ResolveSkillAt`).
- Migrated plugins read durations from `GetTime*`; a unit test with a seeded `SkillDb` row proves a level-2 cast gets the level-2 duration (not the level-1 literal).
- `SkillDbLoader.FromEntity` populates `Time2Ms`/`Time3Ms` from the skill_db `Duration2` column (test on a synthetic row).
- No SC `Start` call in a migrated plugin passes a bare `durationMs` integer literal as the *primary* source (literals survive only as the `?? fallback` after a `GetTime*` read).

## Test plan

- `SkillBehaviorContextWiringTests` — cast a registered SC skill through `SkillCastService` with a real `SkillDb`; assert the plugin received a non-null `ctx.SkillDb`.
- `SkillDbLoaderTests.DurationColumnsMapped` — feed a skill_db entity with `Duration1`/`Duration2` set; assert `GetTime`/`GetTime2`/`GetTime3` return them per level.
- Per-plugin: `MeteorStormTests.StunDurationFromDb` — seed `WZ_METEOR` with distinct level-1 vs level-10 stun durations; assert the proc uses the level-appropriate one.
- Regression: tests that construct `SkillBehaviorContext` by hand still compile (new param is defaulted).

## Worked example — MeteorStorm stun duration

Current: `ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0,0,0, durationMs: 3000, src)`.
The `3000` is a level-independent literal. rAthena's `WZ_METEOR` arm passes
`skill_get_time2(WZ_METEOR, skill_lv)` (the stun window grows with level). After this
ticket: `durationMs: ctx.SkillDb?.GetTime2(SkillId, skillLevel) ?? 3000` — the literal
survives only as the test-path fallback. Combined with SKILL-01 the whole call becomes
`ctx.Sc?.Start(target, StatusType.Stun, rate: 3*skillLevel*100, val1: skillLevel, 0,0,0, ctx.SkillDb?.GetTime2(SkillId, skillLevel) ?? 3000, src)` — one edit, both fixes.

## Audit query

Find the migration surface with:
`grep -rn "durationMs: [0-9]" Map.Server/Skills/Behaviors/` (primary-literal durations)
and `grep -rn "TickCount64 + [a-z0-9 *]*[0-9]" Map.Server/Skills/Behaviors/`
(hardcoded windows like `SevereRainstorm.CanEquipTick`, covered in SKILL-12 X4).
Every hit is a call site to migrate.

## Notes / gotchas

- This depends on the SC apply-rate work (SKILL-01) only in ordering: do SKILL-01's `Start(rate, ...)` signature change first if both land together, so you migrate each call site's duration + rate in one edit instead of twice. They can also land independently.
- `SkillDb.GetTime2`/`GetTime3` clamp out-of-range levels to 0 — a non-zero secondary window must use `?? fallback`, never the bare read, or the SC applies for 0 ms.
- Adding the record param shifts no positional call sites because the two live constructions (`ResolveSkill`/`ResolveSkillAt`) pass it explicitly; hand-built test contexts use the default. Verify the 39→40 param record still compiles everywhere.
- Don't strip the fallback literals from the unit-test path — many existing tests build a context with `SkillDb = null`. The pattern is `GetTime(...) ?? literal`, not `GetTime(...)` alone.
- `GetTime2`/`GetTime3` return 0 for unseeded rows. A plugin that needs a non-zero secondary window must treat 0 as "use fallback," not "duration zero" — otherwise the SC applies for 0 ms and silently no-ops. Make the `?? fallback` explicit for the secondary-window reads.
