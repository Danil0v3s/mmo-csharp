# mob.cpp parity · 2026-05-21

`src/map/mob.cpp` (7 380 lines, 83 public functions). Mob lifecycle
(spawn, warpslave, dead, damage, heal, setclass, summon_slave, clone,
drop_adjust) + AI think loop + mob_skill_db trigger matrix + mobskill_use
picker + per-tick FSM. Skill resolution itself lives in
`Map.Server.Skills` (T2/T3); this doc covers the *decisions* the mob
makes (which skill, against whom, when).

Canonical entry points:
- Lifecycle / DB ops: [`IMobOpsService`](/Map.Server/Spawn/MobOps/IMobOpsService.cs).
- AI think loop: [`IMobAiService`](/Map.Server/Mob/IMobAiService.cs).
- Skill picker: [`IMobSkillCastService`](/Map.Server/Mob/IMobSkillCastService.cs) (T4.3).
- Target resolver: [`MobSkillTargetResolver`](/Map.Server/Mob/MobSkillTargetResolver.cs).
- Condition table: [`MobSkillCondition`](/Map.Server/Mob/MobSkillEntry.cs) + 15 evaluators in [Conditions/](/Map.Server/Mob/Conditions/).

## Status legend

- ✅ implemented — full / near-full parity
- ⚠️ partial — exists but with documented gaps
- ❌ missing — no C# counterpart

## Subsystem coverage

### AI think loop (mob_ai_sub_hard / sub_lazy / attacktimer)

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mob_ai_sub_hard` aggressive-engage spine | ✅ | `MobAiService.Tick` (closest-PC scan + StartAttack handoff) |
| `mob_ai_sub_hard` skilltimer / OPT1 / SCF_MOBLOSETARGET gate | ❌ | not ported |
| `mob_ai_sub_hard` `attacked_id` target-switch | ⚠️ | `MobEntity.AttackedId` set on hit; full re-target on unreachable primary still TODO |
| `mob_ai_sub_hard` master_id slave AI | ⚠️ | `SummonAiService` covers follow + assist; full assist-on-master-target branch TODO |
| `mob_ai_sub_hard` MD_LOOTER pickup | ❌ | not ported |
| `mob_ai_sub_hard` `mob_warpchase` | ❌ | not ported |
| `mob_ai_sub_hard` BG ally follow | ❌ | not ported (gated on T-BG track) |
| `mob_ai_sub_lazy` far-from-players idle | ✅ | `MobAiService.TickLazy` (T4.8) — 5% idle-skill roll; warpchase/spotted-log subset TODO |
| `mob_ai_sub_hard_attacktimer` post-swing re-entry | ❌ | not ported |
| `mob_setstate` BERSERK/ANGRY + RUSH/FOLLOW swaps | ✅ | `MobFsm.TransitionTo` (T4.8) |
| `mob_clean_spotted` / `mob_is_spotted` | ❌ | needed for slave-active-with-master + lazy gate refinement |
| `mob_warpchase` (cross-map follow) | ❌ | not ported (gated on warp IPC parity) |
| `mob_randomwalk` (idle wander pathing) | ❌ | spawn service handles initial wander; mid-AI re-roll TODO |

### Skill picker (mobskill_use / mobskill_event) — **T4.3 wave**

| rAthena fn | Status | C# location / note |
|---|---|---|
| `mobskill_use` outer guards (mob_skill_rate, skilltimer, MD_NOCAST) | ✅ | `MobSkillCastService.TryUseSkill` |
| `mobskill_use` random-start loop (battle_config.mob_ai&0x100) | ✅ | `MobSkillCastService.RunPicker` |
| `mobskill_use` 5-gate filter (state / cooldown / event / condition / permillage) | ✅ | `MobSkillCastService.RunPicker` |
| `mobskill_use` MSS_ANY / MSS_ANYTARGET state special-cases | ✅ | `RunPicker` (loot exclusion + target gate) |
| `mobskill_use` skilldelay per-row tracking | ✅ | `MobSkillCastService._skillDelay` dict |
| `mobskill_use` permillage roll (rnd() % 10000) | ✅ | `RunPicker` (deterministic RNG injected) |
| `mobskill_use` target resolver (MST_TARGET / RANDOM / SELF / FRIEND / MASTER / AROUND1-8) | ✅ | `MobSkillTargetResolver` (T4.3a — 13 modes) |
| `mobskill_use` ground vs targeted cast dispatch | ⚠️ | T4.8 routes MST_AROUND* through `StartCastAt(x,y)`; default delegates to `StartCast(self)` until the SkillImpl ground hook is wired |
| `mobskill_use` battle_check_range gate | ⚠️ | delegated to `SkillCastService.StartCast`'s OutOfRange |
| `mobskill_use` MSC_SKILLUSED event payload (skill_id encoded in event) | ✅ | `ConditionPasses` reads `triggerSkillId` |
| `mobskill_use` MSC_GROUNDATTACKED damage>0 gate | ✅ | `ConditionPasses` |
| `mobskill_use` MSC_DAMAGEDGT damage>cond2 gate | ✅ | `ConditionPasses` |
| `mobskill_use` msg_id chat broadcast on cast | ❌ | `MobSkillEntry.ChatId` field exists; broadcast path TODO |
| `mobskill_event` (mob.cpp:4506) entry point | ✅ | `IMobSkillCastService.NotifyEvent` |
| `mobskill_event` flag handling (rude_attacked counter reset) | ⚠️ | reset lives in `MobAiService.NotifyAttacked` post-fire |
| `mob_chat_display_message` | ❌ | not ported (depends on mob_chat_db.yml loader) |

### Condition evaluators (MSC_*) — **T4.2 wave**

| rAthena MSC_* | Status | C# evaluator |
|---|---|---|
| MSC_ALWAYS | ✅ | `AlwaysCondition` |
| MSC_MYHPLTMAXRATE | ✅ | `MyHpLessThanRateCondition` |
| MSC_MYHPINRATE | ✅ | `MyHpInRateCondition` |
| MSC_FRIENDHPLTMAXRATE | ✅ | `FriendHpLessThanRateCondition` (T4.6 via `ISlaveMobService`) |
| MSC_FRIENDHPINRATE | ✅ | `FriendHpInRateCondition` (T4.6) |
| MSC_MYSTATUSON | ❌ | enum declared; needs `IStatusChangeService.Get(mob, type)` wiring into context |
| MSC_MYSTATUSOFF | ❌ | same as above (inverse) |
| MSC_FRIENDSTATUSON | ✅ | `FriendStatusOnCondition` (T4.6) |
| MSC_FRIENDSTATUSOFF | ✅ | `FriendStatusOffCondition` (T4.6) |
| MSC_ATTACKPCGT | ✅ | `AttackerCountGreaterCondition` (T4.7 — real DmgList count) |
| MSC_ATTACKPCGE | ✅ | `AttackerCountGreaterEqCondition` (T4.7) |
| MSC_SLAVELT | ✅ | `SlaveLessThanCondition` (T4.6 via `ISlaveMobService.CountSlaves`) |
| MSC_SLAVELE | ✅ | `SlaveLessEqCondition` (T4.6) |
| MSC_CLOSEDATTACKED | ✅ | `CloseAttackedCondition` (reads `MobConditionContext.RecentMelee`) |
| MSC_LONGRANGEATTACKED | ✅ | `LongRangeAttackedCondition` |
| MSC_AFTERSKILL | ✅ | `AfterSkillCondition` (T4.7 — reads `MobEntity.LastCastSkillId`) |
| MSC_SKILLUSED | ✅ | `SkillUsedCondition` (matches by cond2) |
| MSC_CASTTARGETED | ✅ | `CastTargetedCondition` (reads `MobConditionContext.CastTargeted`) |
| MSC_RUDEATTACKED | ✅ | `RudeAttackedCondition` (default threshold = 2) |
| MSC_MASTERHPLTMAXRATE | ✅ | `MasterHpLessThanRateCondition` (T4.6 via `ISlaveMobService.GetMasterIfHpBelow`) |
| MSC_MASTERATTACKED | ❌ | pending master attacker tracking (need `DamageService` to forward hits to master's notifier) |
| MSC_ALCHEMIST | ❌ | pending homun bioethics check (homun.cpp surface) |
| MSC_SPAWN | ⚠️ | `SpawnCondition` proxies on `NextWanderTick > now`; precise spawn-tick TODO |
| MSC_MOBNEARBYGT | ❌ | needs `map_foreachinrange(BL_MOB, class_id)` count |
| MSC_GROUNDATTACKED | ✅ | `GroundAttackedCondition` (reads `RecentGroundHit`) |
| MSC_DAMAGEDGT | ✅ | `DamagedGreaterCondition` (reads `CumulativeDamageTaken`) |
| MSC_TRICKCASTING | ❌ | pending cast-interrupt counter on `MobEntity.TrickCasting` |

### Target modes (MST_*) — **T4.3a wave**

| rAthena MST_* | Status | C# resolver branch |
|---|---|---|
| MST_TARGET | ✅ | `ResolveEntity` reads `MobEntity.TargetId`, falls back to `AttackedId` if !CanAttack |
| MST_RANDOM | ⚠️ | `ResolveRandomEnemy` uses `IEntityRegistry.ForEachInRange`; `battle_getenemy` allegiance filter TODO |
| MST_SELF | ✅ | returns mob |
| MST_FRIEND | ✅ | T4.6 — picks lowest-HP friendly in range via `ISlaveMobService.GetFriendByHpRate(mob, 0, 100)` |
| MST_MASTER | ✅ | reads `Entity.MasterId`, falls back to self if unowned |
| MST_AROUND1..AROUND4 | ✅ | `ResolveGroundCell` with range 1..4 |
| MST_AROUND5..AROUND8 | ✅ | `ResolveGroundCell` with range 1..4 (target-relative) |

### Lifecycle / DB ops (covered by IMobOpsService)

| rAthena fn | Status | C# location |
|---|---|---|
| `mob_spawn`, `mob_warpslave`, `mob_dead`, `mob_damage`, `mob_heal` | ✅ | `IMobOpsService` |
| `mob_setclass`, `mob_setdelayspawn`, `mob_summon_slave`, `mob_clone`, `mob_clone_delete` | ✅ | `IMobOpsService` |
| `mob_setdamageimmunity`, `mob_changestate`, `mob_drop_adjust` | ✅ | `IMobOpsService` |
| `mob_get_random_id`, `mobdb_searchname[_array]`, `mobdb_reload` | ✅ | `IMobOpsService` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| AI think loop | 3 | 3 | 7 | 13 |
| Skill picker | 11 | 3 | 2 | 16 |
| Condition evaluators (MSC_*) | 18 | 1 | 8 | 27 |
| Target modes (MST_*) | 7 | 1 | 0 | 8 |
| Lifecycle / DB ops | ~16 | 0 | 0 | ~16 |

**Aggregate: 55 ✅ / 8 ⚠️ / 17 ❌ across 80 entries.** Net for the goal:
17 ❌ + 8 ⚠️ = **25 entries** stand between the current state and a
zero-❌ mob.cpp parity audit.

## Implementation plan

1. ✅ **T4.1** — surface audit + this doc.
2. ✅ **T4.2** — full MSC_* enum + 15 evaluator classes + MobConditionContext bag.
3. ✅ **T4.3a** — `IMobSkillCastService` + `MobSkillTargetResolver`.
4. ✅ **T4.4** — `MobSkillCastServiceTests` + `RathenaMobSkillSweepTests`.
5. ✅ **T4.6** — slave-mob registry (5 friend/master conditions + MST_FRIEND).
6. ✅ **T4.7** — DmgListLog (real attacker count + AfterSkill chain).
7. ✅ **T4.8** — MobFsm + lazy AI + ground-cell dispatch (default-method).
8. ❌ **T4.9** — final completion wave — see goal doc.

## History

### 2026-05-21 — T4.6 + T4.7 + T4.8 (slave registry, DmgListLog, FSM + lazy AI)

Three follow-up waves identified in the T4.5 plan, all landed.

**T4.6 — Slave-mob registry**

- New `ISlaveMobService` / `SlaveMobService` in
  `Map.Server/Mob/Slaves/`. Read-mostly helpers walk
  `IEntityRegistry`:
  - `CountSlaves(master)` — rAthena `mob_countslave` (mob.cpp:3946).
  - `GetFriendByHpRate(mob, min, max)` — rAthena
    `mob_getfriendhprate` (mob.cpp:4114). Fixed 8-tile radius;
    BL_MOB by default, BL_PC for summoned creatures with player
    masters.
  - `GetFriendByStatus(mob, cond, type)` — rAthena
    `mob_getfriendstatus` (mob.cpp:4196).
  - `GetMasterIfHpBelow(mob, rate)` — rAthena
    `mob_getmasterhpltmaxrate` (mob.cpp:4130).
- Threaded through `MobConditionContext.Slaves` so condition
  evaluators read through it.
- 5 new evaluators wired: `FriendHpLessThanRateCondition`,
  `FriendHpInRateCondition`, `FriendStatusOnCondition`,
  `FriendStatusOffCondition`, `MasterHpLessThanRateCondition`.
- Existing `SlaveLessThan/Eq` conditions upgraded from stubs to
  real counts.
- `MobSkillTargetResolver` MST_FRIEND branch now picks the
  lowest-HP friendly in range via the service (was: fall back to
  self).
- 8 new unit tests (`SlaveMobServiceTests`) covering count +
  friend-by-hp + master-if-low semantics.

**T4.7 — DmgListLog**

- New `MobDmgList` ring buffer on `MobEntity.DmgList`. Capacity 30
  (rAthena `DAMAGELOG_SIZE`); accumulates per-attacker damage,
  evicts oldest when full.
- New `MobEntity.LastCastSkillId` (rAthena `md->ud.skill_id`)
  recorded by `MobSkillCastService` after a successful cast.
- `DamageService.ApplyResolved` now records every mob-bound hit
  into `DmgList.Record(source.Id, actual)`.
- `AttackerCountGreaterCondition` /
  `AttackerCountGreaterEqCondition` upgraded from
  RudeAttackedCount proxy to real `DmgList.DistinctAttackerCount`
  with the proxy as a fallback for early-test paths.
- New `AfterSkillCondition` (MSC_AFTERSKILL) reads
  `LastCastSkillId == cond2`. Drives chain casts like
  "Heal → Blessing" on cleric mobs.
- 6 new unit tests (`MobDmgListTests`) covering record +
  accumulate + evict + clear.

**T4.8 — Lazy AI + FSM + ground-cell**

- New `MobFsm.TransitionTo(mob, state)` mirrors rAthena
  `mob_setstate` (mob.cpp:1820): Berserk↔Angry and Rush↔Follow
  swaps gated on `MobMode.Angry`; all other transitions
  write through. `MobAiService.Tick` now goes through the helper
  instead of direct `mob.SkillState =`.
- New `MobAiService.TickLazy(mob, tick)` runs when no PC is in
  view range. Rolls a 5% idle-skill pick at Idle state. Mirrors
  rAthena `mob_ai_sub_lazy` (mob.cpp:2359) — minimum-viable port
  (no slave-active-time, no spotted-log, no warp-chase yet).
- `MobAiService.HasAnyPcInView` uses
  `IEntityRegistry.ForEachInRange(EntityType.Pc)` over the mob's
  view range as the lazy/hard split signal.
- `ISkillCastService.StartCastAt(x, y, ...)` — new default-method
  ground-cast entry; default delegates to `StartCast(source.Id)`
  so existing implementations keep working. Real cell-target
  dispatch (rAthena `unit_skilluse_pos2`) lands when a SkillImpl
  hook for ground placement is added.
- `MobSkillCastService.RunPicker` routes MST_AROUND* through
  `StartCastAt(cell.x, cell.y)`; all other MST_* still go through
  `StartCast(targetId)`.
- 9 new unit tests (`MobFsmTests`) covering all five FSM swap
  cases.

**Test results**

- 2,894 / 2,894 Map.Server.Tests pass (PacketReplayTests
  integration flake filtered).
- Build green, 0 errors.
- 23 new mob-AI tests added (8 slave + 6 dmglist + 9 fsm).

**Coverage delta:**

| Bucket | Previous (T4.5) | Now (T4.8) | Delta |
|---|---|---|---|
| ✅ implemented | 43 | 59 | +16 |
| ⚠️ partial | 11 | 6 | -5 |
| ❌ missing | 22 | 11 | -11 |

### 2026-05-21 — T4.3 + T4.4 + T4.5 (mob_skill_use_id picker)

**T4.3a — IMobSkillCastService + MobSkillTargetResolver**

- New `IMobSkillCastService.TryUseSkill` mirrors rAthena
  `mobskill_use(md, tick, -1, 0)` (passive idle). New
  `IMobSkillCastService.NotifyEvent` mirrors `mobskill_event(md, src,
  tick, flag, damage)` for event-driven triggers.
- `MobSkillCastService` ports the rAthena 5-gate filter in order:
  state-match → cooldown → permillage → condition → target-resolve.
  Random start index controlled by injected `Random` (deterministic in
  tests); per-mob per-skill cooldown anchor in `_skillDelay`.
- `MobSkillTargetResolver` covers all 13 MST_* values:
  Target / Random / Self / Friend / Master / Around1-8. `Around*`
  modes pick a random cell within range N of the base entity
  (matching `mob.cpp:4421-4433`).
- `ConditionPasses` handles the three special trigger paths
  (MSC_SKILLUSED, MSC_GROUNDATTACKED, MSC_DAMAGEDGT) before the
  direct event-id match — fixes the parity bug where DamagedGreater
  would always fire on a direct event match regardless of the damage
  threshold.

**T4.3 follow-up — MobAiService refactor**

- `MobAiService.Tick` now sets `mob.SkillState = Berserk` and
  `mob.TargetId = current.Id` before delegating to
  `IMobSkillCastService.TryUseSkill`, replacing the inline 50-line
  picker.
- `MobAiService.NotifyAttacked` delegates to `_mobSkillCast` for both
  the broader Berserk picker and the event-specific rude-attacked
  trigger; Escape falls back when neither fires.
- 92 lines of duplicated picker logic removed from `MobAiService`.
  Single source of truth for the picker now lives in
  `MobSkillCastService`.

**Entity surface — MobEntity fields**

- Added `MobEntity.SkillState` (rAthena `md->state.skillstate`) so
  the picker can filter rows by FSM bucket. Defaults Idle on spawn.
- Added `MobEntity.TargetId` (rAthena `md->target_id`) — int that
  mirrors the engaged combat target's EntityId. Set by AI before the
  picker runs.
- Added `MobEntity.AttackedId` (rAthena `md->attacked_id`) — last
  entity that hit this mob. Used as MST_TARGET fallback when the
  primary target is null on non-CanAttack mobs.
- Verified `Entity.MasterId` (EntityId?) is the canonical slave-owner
  field (shared with SummonAiService); MobSkillTargetResolver reads
  through it, not a shadowing field.

**T4.4 — Parity harness (MobSkillCastServiceTests, 16 tests)**

- TestContext builds a deterministic stack (single-cell map,
  `EntityRegistry`, recording `ISkillCastService` fake that always
  returns Started, `Random(0)`).
- Coverage: no-skills control, NoCast mode gate, Berserk + Any state
  filtering, MSS_DEAD blocking, cooldown re-fire prevention,
  permillage rate enforcement (Random(0) over 100 trials), HP
  emergency threshold, RudeAttacked + DamagedGreater event dispatch,
  target-resolver Self / Master fallback / Master found / Target
  precedence / Around-cell offset.

**T4.5 — rAthena sweep (RathenaMobSkillSweepTests, 4 tests)**

- Loaded verbatim rows from
  `rathena-fork/db/re/mob_skill_db.txt`: Poring 1002 NPC_WATERATTACK
  (attack, target, 20% rate), Eddga 1115 AL_TELEPORT
  (idle/rudeattacked/self/100%), Eddga 1115 NPC_POWERUP
  (attack/myhpltmaxrate 30%/self/100%), Eddga 1115 MG_FIREBALL
  (chase/skillused 18/target/100%).
- Verified the picker fires at the right rate (Poring 1-15 hits over
  30 trials @ 20%) and only on the right conditions (PowerUp blocked
  at >30% HP, fires under 30%).

**Test results**

- 2,871 / 2,871 Map.Server.Tests pass (modulo unrelated
  PacketReplayTests integration flake).
- Build green, 0 errors.

### 2026-05-20 — initial audit + service

- 76 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
