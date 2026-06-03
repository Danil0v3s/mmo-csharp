# MOBAI-01 — Slave→master coupling in the hard-AI tick

> **Epic:** Mob AI parity · **Status:** ✅ Done (2026-06-03) · **Size:** M · **Player-visible:** yes
> **Depends on:** none · **Blocks:** MOBAI-03

## Problem

Mob slaves (the adds a boss summons, e.g. Osiris/Baphomet/Doppelganger via
`NPC_SUMMONSLAVE`) currently behave like ordinary wild mobs once spawned. They do
**not** follow their master, do **not** inherit the master's combat target, and
the master-summon-replenish loop only fires opportunistically through the skill
picker rather than as part of the documented AI path. Result: a player kites a
boss across the map and its slaves stay behind on their spawn cell; slaves
ignore the player the boss is fighting; a boss whose slaves are all dead does
not reliably re-summon because the slave-coupling pass that rAthena runs *before*
the aggro scan never executes.

The C# `MobAiService.Tick` has every other hard-AI branch (lazy/hard split,
OPT1 lose-target, target validation, looter scan, aggressive scan, random walk)
but is missing the `mob_ai_sub_hard_slavemob` branch entirely. `SlaveMobService`
exists but only exposes read-only lookups (`CountSlaves`, `GetFriendByHpRate`,
`GetMasterIfHpBelow`); nothing drives slave *movement* or *target inheritance*
from the AI tick. `SummonAiService` handles *player* summons (pets/homun/merc by
`MasterId`) but explicitly does not run for mob-mastered slaves that are
themselves `MobEntity` instances in the mob registry pass.

## Current state (C#)

- `Map.Server/Mob/MobAiService.cs:102 Tick(long nowTick)` — the hard-AI loop. After
  target validation (`:164-183`) it falls straight into the looter scan
  (`:189-210`) and aggressive scan (`:212-248`). **There is no `md->master_id > 0`
  branch** between target validation and the looter scan, where rAthena runs
  `mob_ai_sub_hard_slavemob`.
- `Map.Server/Mob/Slaves/SlaveMobService.cs` — read-only helpers only:
  `CountSlaves` (`:29`), `GetFriendByHpRate` (`:43`), `GetFriendByStatus` (`:71`),
  `GetMasterIfHpBelow` (`:88`). No follow / no target-inherit / no teleport-to-master.
- `Map.Server/Mob/Slaves/ISlaveMobService.cs` — interface; no `TickSlave` /
  `FollowMaster` / `InheritMasterTarget` method.
- `Map.Server/Mob/SummonAiService.cs:49 Tick` — follows/assists by `MasterId`, but it
  **despawns** any entity whose master is off-map (`:62-69`) and is designed for
  player summons; it does not implement rAthena's slave-distance / link-time
  semantics (MOB_SLAVEDISTANCE=2, MIN_MOBLINKTIME=300ms) and is not the path the
  mob-registry tick walks for mob slaves.
- `Map.Server/Entities/Entity.cs:84 EntityId? MasterId` — set on slaves at
  `Map.Server/Spawn/MobSpawnService.cs:148 mob.MasterId = masterId`. There is **no**
  `MasterDist` or `LastLinkTime` field on `MobEntity`
  (`Map.Server/Entities/MobEntity.cs`).
- `Map.Server/Movement/IMovementService.TryStartWalk(entity, x, y)` — the walk
  primitive used elsewhere in `MobAiService` (`:206`, `:450`). Available.
- The replenish skill (`NPC_SUMMONSLAVE`) is seeded with `slavele`/`slavelt`
  conditions (see `Seeds/Scripts/seed_mob_skill_db.sql`); the `SlaveLessThanCondition`
  / `SlaveLessEqCondition` evaluators are already registered in
  `MobAiService` ctor (`:77-78`) and read through `ISlaveMobService.CountSlaves`.
  So replenish *can* fire via the skill funnel today, but only when the picker is
  reached — it is **not** anchored to the slave-coupling pass and does not run for a
  slave that died this tick.

## rAthena reference (source of truth)

Canonical: `rathena/src/map/mob.cpp` (monolithic; no split files).

- `mob.cpp:1449 mob_ai_sub_hard_slavemob(md, tick)` — the slave coupling routine,
  invoked from `mob_ai_sub_hard` at `mob.cpp:1857-1861`:
  ```c
  if (md->master_id > 0) {
      if (mob_ai_sub_hard_slavemob(md, tick) == 1)
          return true;          // slave handled this tick (walked/warped/inherited)
      slave_lost_target = true; // slave with no master action → may aggro
  }
  ```
  The `slave_lost_target` flag then *forces* the aggressive scan even if the
  slave already had a (now-cleared) target (`mob.cpp:1870`).
- `mob_ai_sub_hard_slavemob` body (`mob.cpp:1449-1533`):
  1. `bl = map_id2bl(md->master_id)`. If master gone / dead → `status_kill(slave)`
     (slave dies with its master) → return 1. If master `prev==nullptr`
     (warping) → return 0 (do nothing this tick).
  2. If slave has `MD_CANMOVE`:
     - `md->master_dist = distance_bl(slave, master)` (Chebyshev).
     - If `slave_stick_with_master` (or AI_ABR/AI_BIONIC): if master on a
       different map **or** `master_dist > AREA_SIZE+1` → `unit_warp` slave to
       master's cell (CLR_TELEPORT) → return 1.
     - **Target busy:** if `md->target_id` set: for a **player-mastered** slave
       (`bl->type==BL_PC`) with `master_dist > 5` → drop target
       (`mob_unlocktarget`) and `unit_walktobl(master, MOB_SLAVEDISTANCE, 1)` →
       return 1. Otherwise (mob master, or close enough) → return 0 (keep fighting).
     - **Approach master:** if `master_dist > MOB_SLAVEDISTANCE (2)` **or**
       `master_dist == 0` (standing on master) and the slave can move:
       `map_search_freecell` near master within MOB_SLAVEDISTANCE, then
       `unit_walktoxy`. On success → stop attacking, return 1. On freecell-found
       but walk-fail → `unit_stop_walking(FIXPOS)`, return 0 (target re-picked when
       back in range).
  3. **Target inheritance** (`mob.cpp:1508-1531`): throttled by
     `MIN_MOBLINKTIME (300ms)` via `md->last_linktime`, and only when the slave has
     **no** target. Reads the master's `unit_data`:
     - `ud->target` (and `state.attack_continue`) → candidate `tbl`,
     - else `ud->target_to` (chase target) → `tbl`,
     - else `ud->skilltarget` → `tbl` **but** only if
       `battle_check_target(slave, tbl, BCT_ENEMY) > 0` (skilltarget can be an ally).
     If a candidate survives `status_check_skilluse(slave, tbl)` →
     `md->target_id = tbl->id`; return 1.
  4. Otherwise return 0.
- Constants: `MOB_SLAVEDISTANCE = 2` (`mob.hpp:44`), `MIN_MOBLINKTIME = 300`
  (`mob.hpp:36`), `AREA_SIZE = battle_config.area_size` (default 14, `map.hpp:60`),
  `RUDE_ATTACKED_COUNT = 1` (`mob.cpp:49`).
- **Replenish:** rAthena does not replenish inside `mob_ai_sub_hard_slavemob`; it
  re-summons via the master's `NPC_SUMMONSLAVE` skill row, gated by the
  `slavele`/`slavelt` (MSC_SLAVELT, `mob.cpp:4007`) conditions which count live
  slaves with `mob_countslave`. The slave's *death* makes the count drop; the next
  master think-tick re-rolls the summon. So the "replenish in the AI path" gap is
  really: (a) ensure the slave-death decrements the master's live-slave count
  reachable by `CountSlaves` (it does — registry scan), and (b) ensure the master's
  skill picker is reached every think-tick (it is, via the engaged-target arm).
  This ticket must **verify** the replenish round-trips end-to-end after wiring the
  coupling pass; no new replenish code path is expected, but if a slave death does
  not lower `CountSlaves` (e.g. dead-but-not-removed), fix that.

## Scope — every sub-system that must be touched

- [x] **Field additions** — `MobEntity.MasterDist` + `MobEntity.LastLinkTime` (transient AI state).
- [x] **`ISlaveMobService.TickSlave` + `SlaveMobService`** — implemented the `mob_ai_sub_hard_slavemob`
      body returning `SlaveTickResult { Handled, Continue, MasterGone }`. MasterGone → caller sets
      Hp 0 + `IMobDeathSink.KillMob(id, null)`; Handled → `continue` (+ engage an inherited target);
      Continue → `slave_lost_target = true` + fall through to looter/aggro.
- [x] **Follow logic** — compute `MasterDist`; walk toward master when `MasterDist > MOB_SLAVEDISTANCE
      (2) || == 0`. The unconditional warp arm is gated on `slave_stick_with_master` (rAthena default
      **off** → only ABR/Bionic summons warp; ordinary boss adds walk) — hardcoded off + documented in
      the code. No freecell primitive → walk to the master's cell (the movement layer resolves the
      nearest walkable); noted as the parity-neutral pile-on divergence.
- [x] **Target inheritance** — throttled on `nowTick - LastLinkTime >= MIN_MOBLINKTIME (300)`, only when
      idle; resolves the master's target (`Attack.TargetId`, else `MobEntity.TargetId/AttackedId`),
      validates it as an alive same-map PlayerEntity (the mob-slave enemy case), sets `TargetId` +
      `LastLinkTime`, and the AI tick starts the attack so the slave engages.
- [x] **Wired into `MobAiService.Tick`** after the engaged-target `continue`, before the looter scan;
      threaded `slaveLostTarget` into the aggro gate (an aggressive **or** slave-lost-target mob scans).
      Also: a mob with a master now always runs the **hard** path (bypasses the lazy/no-PC shortcut)
      so slaves follow a master kited away from any PC.
  - [ ] ➡️ The engaged player-mastered slave's leash-back (target-busy `> 5` drop) is unreachable with
        the "after the engaged-continue" placement — split to **MOBAI-06** (dormant: no spawn path
        produces a player-mastered *mob* slave today; the TickSlave arm is present but un-wired).
- [x] **Replenish verification** — a slave death (Hp 0) drops `CountSlaves` (it filters `Hp<=0`),
      re-firing the master's `NPC_SUMMONSLAVE` via the existing `SlaveLessThan` skill condition
      (no new replenish path). Verified by a test.
- [x] **DI / ctor** — `MobAiService` injects `ISlaveMobService` + `IMobDeathSink` (both already DI-
      registered; auto-resolved in production), defaulting to an inline `SlaveMobService` so the
      existing test ctor keeps working.
- [x] No EF migration, no packets.

### Original scope (reference)

- [ ] **Field additions** on `Map.Server/Entities/MobEntity.cs`: `int MasterDist`
      (last computed Chebyshev distance to master) and `long LastLinkTime` (the
      `md->last_linktime` throttle anchor). No persistence — transient AI state.
- [ ] **`ISlaveMobService` + `SlaveMobService`** — add
      `SlaveTickResult TickSlave(MobEntity slave, long nowTick)` implementing the
      `mob_ai_sub_hard_slavemob` body above. Return an enum
      `{ Handled, Continue, MasterGone }` so the AI tick can branch the way rAthena
      branches on the `0/1`/`status_kill` return:
  - `MasterGone` → caller kills the slave (HP→0 + KillMob with null lastHitter so
    no exp/loot credit, matching `status_kill`); rAthena `status_kill(slave)` when
    master dead/gone.
  - `Handled` → `return` (skip the rest of this slave's tick), and set
    `slave_lost_target = true` for the aggro override (see next item).
  - `Continue` → fall through to looter/aggressive scan.
- [ ] **Follow logic** inside `TickSlave`: compute `MasterDist`; teleport to master
      if cross-map or `MasterDist > AREA_SIZE+1` (only under stick-with-master /
      summon-AI semantics — gate on a `slave_stick_with_master` config bool plumbed
      through, default the rAthena default which is off, so the warp arm only fires
      for ABR/Bionic-style summons; document the config); otherwise
      `_movement.TryStartWalk(slave, freeCellX, freeCellY)` toward master when
      `MasterDist > MOB_SLAVEDISTANCE || MasterDist == 0`. Player-mastered slave with
      a target and `MasterDist > 5` drops target and walks back.
      Use `map_search_freecell` analogue — if no freecell helper exists, walk to the
      master's exact cell (the movement service resolves the nearest walkable),
      and note the divergence.
- [ ] **Target inheritance** inside `TickSlave`: throttle on
      `nowTick - slave.LastLinkTime >= MIN_MOBLINKTIME (300)` and only when
      `slave.TargetId == 0`. Read the master's combat target. Masters can be
      `PlayerEntity` (`Combat.AttackState` / `unit` target) or `MobEntity`
      (`MobEntity.TargetId` / `Attack.TargetId`). Resolve the master's current
      target id, validate it is an enemy of the slave
      (`IBattleTargetService`/`battle_check_target` analogue) and reachable, then set
      `slave.TargetId`. Set `slave.LastLinkTime = nowTick`.
- [ ] **Wire into `MobAiService.Tick`**: insert the slave branch **after** target
      validation (`MobAiService.cs:183`, after the engaged-target `continue`) and
      **before** the looter scan (`:189`). Mirror rAthena's
      `if (md->master_id > 0) { ... slave_lost_target = true; }` and thread
      `slaveLostTarget` into the aggressive-scan gate at `:212` so an aggressive
      slave that just lost its target still re-scans this tick
      (`(mode & Aggressive) != 0 && (closest-scan) || slaveLostTarget`).
- [ ] **Master-summon-replenish verification**: after wiring, confirm a slave
      death lowers `ISlaveMobService.CountSlaves(master)` and the master re-summons
      via the `NPC_SUMMONSLAVE` row on its next think. If the slave entity lingers in
      the registry post-death (HP 0 but not removed) and inflates the count, exclude
      `Hp<=0` from `CountSlaves` (it already filters `m.Hp <= 0` at
      `SlaveMobService.cs:36` — confirm the death path zeroes HP before respawn-removal).
- [ ] **DI / ctor**: `MobAiService` must take `ISlaveMobService` (it currently does
      not inject it — only the picker does). Add the optional ctor param + register;
      keep the existing test ctor working by defaulting to a `SlaveMobService` built
      over `IEntityRegistry`.
- [ ] No EF migration, no packets — pure server-side AI state.

## Done criteria

- ✅ A boss + slave spawn: kiting the boss makes each living slave path toward the boss when its
  Chebyshev distance exceeds 2 (`Slave_follows_master_when_out_of_slave_distance`).
- ✅ An idle slave inherits the boss's current target within one `MIN_MOBLINKTIME` window and begins
  attacking it; it does NOT inherit before 300ms (`Idle_slave_inherits_master_target_after_the_link_throttle`).
- ✅ Killing the boss kills its living slaves on the slaves' next tick — no orphans
  (`Slave_dies_when_its_master_is_gone`).
- ✅ A slave death drops the master's live-slave count, re-firing the `NPC_SUMMONSLAVE` summon
  condition (`Slave_death_lowers_the_masters_live_slave_count`).
- ➡️ A player-mastered slave > 5 cells from its player master abandons its target — **MOBAI-06**
  (unreachable with the ticket's "after the engaged-continue" placement; dormant — no spawn path
  produces a player-mastered mob slave today; the TickSlave arm is implemented but un-wired).
- ✅ No `// TODO`, no log-only no-op in `Tick` / `TickSlave`.

## History

- 2026-06-03 — Ported `mob_ai_sub_hard_slavemob`: added `MobEntity.MasterDist`/`LastLinkTime`,
  `ISlaveMobService.TickSlave` (+ `SlaveTickResult`), and wired the slave branch into
  `MobAiService.Tick` (after target validation, before the looter/aggro scans). Slaves now follow a
  kited master (walk when > 2 cells away), inherit the master's target (throttled 300ms) and engage
  it, die with the master (`IMobDeathSink.KillMob`, no exp credit), and force the aggro scan when
  slave-lost-target. A mob with a master always runs the hard path so it follows even with no PC in
  view. `MobAiServiceTests` +6; full Map.Server.Tests 4266 pass (1 fail = pre-existing INFRA-11 replay
  gate). Filed MOBAI-06 for the engaged player-mastered-slave leash-back (unreachable with the
  ticket's placement; dormant today).

## Test plan

- `Map.Server.Tests` `SlaveMobAiTests` (new):
  - **follow**: seed master + slave 6 cells apart, run `Tick`, assert
    `_movement.TryStartWalk` invoked toward master and `MasterDist` updated.
  - **target inherit**: master engaged on a PC, slave idle, advance tick past
    300ms link throttle → assert `slave.TargetId == pc.Id`; assert it does NOT
    re-inherit before 300ms elapse.
  - **master gone**: remove master, run `Tick` → assert slave HP→0 / removed and
    no exp/loot credited.
  - **aggro override**: aggressive slave that just lost its target still runs the
    active scan in the same tick (`slaveLostTarget` true).
  - **replenish round-trip**: master with `slavele 3 NPC_SUMMONSLAVE` row + 3
    slaves; kill one; advance ticks; assert `CountSlaves` returns to 3.
- Regression: existing `MobAiService` aggro/looter/wander tests still pass (the
  slave branch must not fire for `MasterId == null` mobs).
- Manual/live: spawn `@monster Baphomet`, kite it, confirm slaves follow and the
  boss re-summons murders.

## Notes / gotchas

- rAthena's slave branch can `return true` (Handled) **before** the aggro scan —
  but it sets `slave_lost_target` so an aggressive slave that wasn't handled still
  scans. Reproduce both branches exactly; do not collapse them.
- `map_search_freecell` (rAthena) keeps slaves from stacking on the master's exact
  cell. If the C# movement layer has no freecell primitive, the slaves will pile on
  the master cell; flag this as a parity-neutral follow-up but pick a cell offset by
  1 when possible.
- Distance is **Chebyshev** (`distance_bl` = `max(|dx|,|dy|)`), matching the rest
  of `MobAiService`. Do not use Euclidean.
- `slave_stick_with_master` (battle_config) is **off** by default; the unconditional
  teleport arm should only trigger for AI_ABR/AI_BIONIC-style summons. For ordinary
  boss adds the warp arm stays dormant and slaves walk. Plumb the config or hardcode
  the rAthena default and document it.
- Do not route mob-mastered slaves through `SummonAiService` — that ticker is for
  player summons and despawns on master-off-map, which is wrong for mob slaves
  (they teleport, not despawn, under stick-with-master). Keep the two paths separate.
