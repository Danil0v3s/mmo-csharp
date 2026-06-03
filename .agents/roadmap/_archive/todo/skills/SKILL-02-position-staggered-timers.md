# SKILL-02 — Position-targeted staggered timers (meteor / comet trains)

> **Epic:** Skills · **Status:** ❌ Not started · **Size:** L · **Player-visible:** yes
> **Depends on:** none · **Blocks:** SKILL-12 (Mage depth)

## Problem

rAthena schedules many AoE skills as a *train* of position-targeted delayed hits:
each meteor / each comet wave / each Gravitation pulse fires `skill_addtimerskill`
at a staggered `tick + offset` with a **ground (x,y)** target instead of an entity.
The C# `ISkillTimerService.Schedule` only takes a `target` *Entity* — there is no
position-targeted timer. As a result every plugin that should stagger drops all its
sub-units in the **same tick**. Meteor Storm dumps `2 + lv` meteors simultaneously
instead of raining them over the cast duration; the visual + the damage cadence are
both wrong, and skills whose later waves depend on the earlier wave's position
(comet split, Gravitation) cannot exist.

`MeteorStorm.cs` documents the gap inline: *"INFRA-DEFERRED: rAthena staggers each
meteor via `skill_addtimerskill` with a position-targeted timer; our
`ISkillTimerService` only schedules entity-targeted callbacks. We drop all meteors
in the same tick."* `SkillUnitGroup.StartAt` exists for whole-group deferral but
there is no **per-position-within-a-cast** stagger.

## Current state (C#)

- `Map.Server/Skills/SkillTimerService.cs:31` — `Schedule(Entity src, Entity target, int delayMs, ushort skillId, ushort skillLevel, Action<Entity,Entity,ushort> callback)`. Stores `SourceId`/`TargetId` only; resolves both via `IEntityRegistry.Get` at fire time and drops the entry if `target == null`. No (x,y) carrier.
- `Map.Server/Skills/SkillTimerService.cs:61-64` — fire-time guards: src/target null-check + same-map check. A position timer has no target entity, so it can't pass this.
- `Map.Server/Skills/SkillUnitGroup.cs:45` — `StartAt` defers an *entire* group; comment says it's for "staggered sub-unit spawns" but it's group-granular, not per-position.
- `Map.Server/Skills/Behaviors/Mage/MeteorStorm.cs` `CastendPos2` — for-loop calls `_units?.Place(src, SkillId, skillLevel, ox, oy)` for all `2 + lv` meteors in one tick; documents the deferral.
- `Map.Server/Skills/Behaviors/Novice/JupitelThunderstorm.cs`, `GroundGravitation.cs`, comet/quake `_ATK` plugins — same "all sub-hits this tick" shape.

## rAthena reference (source of truth)

- `rathena/src/map/skill.hpp:561` — `int32 skill_addtimerskill(struct block_list *src, t_tick tick, int32 target, int32 x, int32 y, uint16 skill_id, uint16 skill_lv, int32 type, int32 flag)`. Note `int32 x, int32 y` AND `int32 target`: when `target == 0` the timer is **position-targeted** and fires at (x,y).
- `rathena/src/map/skill.hpp:359` — `struct TimerSkill { ... int16 x,y; ... }` — the timer entry carries the ground coords.
- `rathena/src/map/skill.cpp:15065` / `:15277` / `:15289` — staggered position timers: `skill_addtimerskill(src, gettick() + (140 * w), 0, sx, sy, skill_id, skill_lv, dir, flag&2)` — note `target = 0`, `(sx,sy)` set, and `w` is the per-wave index so the offset grows per wave (the "train").
- `rathena/src/map/skill.cpp:4896` `skill_addtimerskill` body + `skill_timerskill` consumer (≈4640) — when `target == 0` it runs the pos branch (`skill_castend_pos2` / `skill_unit_setting`) at (x,y); when `target != 0` it runs the entity branch. The dead-target guard only applies to the entity branch.

## Scope — every sub-system that must be touched

- [ ] **`ISkillTimerService` position overload** — add `SchedulePos(Entity src, uint mapId, short x, short y, int delayMs, ushort skillId, ushort skillLevel, Action<Entity, short, short, ushort> callback)`. No target entity; fires the callback with (src, x, y, level) when due.
- [ ] **`SkillTimerService` impl** — add a `PendingPos` record (`SourceId`, `MapId`, `X`, `Y`, `FireTick`, `SkillId`, `SkillLevel`, callback). Drain it in `Tick` alongside the existing entity queue. Fire-time guard: resolve `src` only (`src == null` → drop, `src.MapId != MapId` → drop); **no target guard** (rAthena's pos branch skips the target-dead check). Wrap callback in the same try/catch+log.
- [ ] **`MeteorStorm` rewrite** — replace the same-tick for-loop with a `SchedulePos` train: meteor `i` fires at `delay = i * METEOR_INTERVAL` (rAthena uses a per-meteor poll cadence; pin the interval to the `skill_db` `WZ_METEOR` unit interval / the `skill.cpp` arm value) and places its ground unit at the rolled offset on fire. Remove the INFRA-DEFERRED doc note.
- [ ] **Comet / quake / Gravitation trains** — audit `Novice/JupitelThunderstorm`, `GroundGravitation`, and the `AG_*_ATK` / `WL_COMET`-family plugins; convert any "all sub-hits this tick" loop to a `SchedulePos` train where rAthena staggers. Each gets the rAthena per-wave offset formula cited in its docstring.
- [ ] **Game-loop wiring** — confirm `SkillTimerService.Tick(nowTick)` is already pumped from the map loop (it is, alongside the existing entity queue); the new pos queue rides the same `Tick`. No new loop registration.
- [ ] **Client visuals** — the placement call (`ISkillUnitService.Place`) already broadcasts the unit-place packet; staggering the `Place` calls is what produces the staggered visual. No new packet.
- [ ] **No new DB / IPC.**

## Done criteria

- `SchedulePos` fires its callback at `src` map (x,y) with no target entity required; a unit test with a seeded clock asserts callbacks fire at the staggered ticks, not all at tick 0.
- Meteor Storm places its `2 + lv` meteors across `(2+lv) * interval` ms, not in a single tick (test asserts placement ticks are distinct and monotonic).
- A position timer survives the target-dead case (no entity to die) — test schedules a pos timer with no nearby entity and asserts it still fires.
- No `INFRA-DEFERRED` / "drop them all in the same tick" comment remains in `MeteorStorm.cs` or the converted train plugins.

## Test plan

- `SkillTimerServiceTests.SchedulePos_FiresAtStaggeredTicks` — schedule 3 pos timers at 0/100/200 ms; advance the clock in steps; assert each fires once, in order.
- `SkillTimerServiceTests.SchedulePos_NoTargetGuard` — a pos timer fires even with no target entity registered.
- `MeteorStormTests.Stagger` — cast at (x,y); assert the placement service receives `2 + lv` calls at distinct, increasing ticks.
- Regression for any other converted train plugin (Gravitation pulse cadence).

## Worked example — Meteor Storm train

rAthena `WZ_METEOR` drops `2 + skill_lv` meteors. Each meteor is a position timer at
`tick + (i * interval)` where `interval` is the per-meteor poll cadence from the arm
(the C# `MeteorStorm.cs` currently uses a flat 9×9 envelope, `Half = 4`). On fire,
the callback rolls a fresh `(ox, oy)` offset inside the envelope and calls
`ISkillUnitService.Place(src, WZ_METEOR, lv, ox, oy)` — which itself broadcasts the
unit-place packet, so the *visual* staggers because the `Place` calls stagger. The
per-cell damage interval (units ticking the victims under them) is a separate cadence
owned by `SkillUnitGroup.IntervalMs` and is unaffected by this change.

Convert: replace the `for (i…) _units.Place(…)` loop with
`for (i…) ctx.??.SchedulePos(src, src.MapId, x, y, i*interval, WZ_METEOR, lv, (s,fx,fy,l) => place rolled offset)`.
The timer service must be reachable from the plugin — confirm it's exposed on
`SkillBehaviorContext` (it is plumbed via the cast service's `Tick`); if the plugin
currently takes `ISkillUnitService` by ctor, add `ISkillTimerService` the same way.

## Notes / gotchas

- Keep the existing entity-targeted `Schedule` untouched — Sonic Blow's 8-hit ladder and other entity-targeted chains rely on it and on the target-dead drop. The pos overload is additive.
- `SchedulePos` must NOT apply the same-map guard against a *target* (there is none); it guards only `src`. Copying the entity-path guard verbatim would drop every pos timer.
- The closure captures the wave index `i` — that's how the C# avoids rAthena's `type+1` increment. Don't re-introduce the integer `type` ladder.
- The `type`/`flag` int pair in rAthena's `skill_addtimerskill` encodes the wave index + alt-dmg flag; the C# closure captures the wave index directly (per the SkillTimerService docstring's "closure variables" convention). Don't re-introduce a `type`/`flag` int pair.
- Pin the per-wave interval from the actual `skill.cpp` arm (`140 * w` / `80 * w` style) or the `skill_unit_db` interval — do not invent a constant. Cite the source line in the plugin docstring.
