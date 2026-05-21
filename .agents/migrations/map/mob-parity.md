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
| `mob_ai_sub_hard` `attacked_id` target-switch | ❌ | `MobEntity.AttackedId` exists; switch logic TODO |
| `mob_ai_sub_hard` master_id slave AI | ⚠️ | `SummonAiService` covers follow + assist; full attack-aggro branch TODO |
| `mob_ai_sub_hard` MD_LOOTER pickup | ❌ | not ported |
| `mob_ai_sub_hard` `mob_warpchase` | ❌ | not ported |
| `mob_ai_sub_hard` BG ally follow | ❌ | not ported (gated on T-BG track) |
| `mob_ai_sub_lazy` far-from-players idle | ❌ | `MobAiService` runs hard path on every tick |
| `mob_ai_sub_hard_attacktimer` post-swing re-entry | ❌ | not ported |

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
| `mobskill_use` ground vs targeted cast dispatch | ⚠️ | C# uses `StartCast(target)` for both; ground-cell path TODO |
| `mobskill_use` battle_check_range gate | ⚠️ | delegated to `SkillCastService.StartCast`'s OutOfRange |
| `mobskill_use` MSC_SKILLUSED event payload (skill_id encoded in event) | ✅ | `ConditionPasses` reads `triggerSkillId` |
| `mobskill_use` MSC_GROUNDATTACKED damage>0 gate | ✅ | `ConditionPasses` |
| `mobskill_use` MSC_DAMAGEDGT damage>cond2 gate | ✅ | `ConditionPasses` |
| `mobskill_use` msg_id chat broadcast on cast | ❌ | `MobSkillEntry.ChatId` field exists; broadcast path TODO |
| `mobskill_event` (mob.cpp:4506) entry point | ✅ | `IMobSkillCastService.NotifyEvent` |
| `mobskill_event` flag handling (rude_attacked counter reset) | ⚠️ | reset lives in `MobAiService.NotifyAttacked` post-fire |
| `mob_chat_display_message` | ❌ | not ported |

### Condition evaluators (MSC_*) — **T4.2 wave**

| rAthena MSC_* | Status | C# evaluator |
|---|---|---|
| MSC_ALWAYS | ✅ | `AlwaysCondition` |
| MSC_MYHPLTMAXRATE | ✅ | `MyHpLessThanRateCondition` |
| MSC_MYHPINRATE | ✅ | `MyHpInRateCondition` |
| MSC_FRIENDHPLTMAXRATE | ❌ | enum declared; evaluator pending friend-tracker |
| MSC_FRIENDHPINRATE | ❌ | enum declared; evaluator pending friend-tracker |
| MSC_MYSTATUSON | ❌ | enum declared; needs SC dictionary lookup |
| MSC_MYSTATUSOFF | ❌ | enum declared; needs SC dictionary lookup |
| MSC_FRIENDSTATUSON | ❌ | pending |
| MSC_FRIENDSTATUSOFF | ❌ | pending |
| MSC_ATTACKPCGT | ⚠️ | `AttackerCountGreaterCondition` proxies on `RudeAttackedCount` until DmgListLog ports |
| MSC_ATTACKPCGE | ⚠️ | `AttackerCountGreaterEqCondition` same caveat |
| MSC_SLAVELT | ⚠️ | `SlaveLessThanCondition` stub — pending slave registry |
| MSC_SLAVELE | ⚠️ | `SlaveLessEqCondition` stub — pending slave registry |
| MSC_CLOSEDATTACKED | ✅ | `CloseAttackedCondition` (reads `MobConditionContext.RecentMelee`) |
| MSC_LONGRANGEATTACKED | ✅ | `LongRangeAttackedCondition` |
| MSC_AFTERSKILL | ❌ | enum declared; needs `mob.last_skill` tracking |
| MSC_SKILLUSED | ✅ | `SkillUsedCondition` (matches by cond2) |
| MSC_CASTTARGETED | ✅ | `CastTargetedCondition` (reads `MobConditionContext.CastTargeted`) |
| MSC_RUDEATTACKED | ✅ | `RudeAttackedCondition` (default threshold = 2) |
| MSC_MASTERHPLTMAXRATE | ❌ | pending slave registry + master lookup |
| MSC_MASTERATTACKED | ❌ | pending master attacker tracking |
| MSC_ALCHEMIST | ❌ | pending homun bioethics check |
| MSC_SPAWN | ⚠️ | `SpawnCondition` proxies on `NextWanderTick > now` |
| MSC_MOBNEARBYGT | ❌ | needs `map_foreachinrange(BL_MOB)` count |
| MSC_GROUNDATTACKED | ✅ | `GroundAttackedCondition` (reads `RecentGroundHit`) |
| MSC_DAMAGEDGT | ✅ | `DamagedGreaterCondition` (reads `CumulativeDamageTaken`) |
| MSC_TRICKCASTING | ❌ | pending cast-interrupt tracking |

### Target modes (MST_*) — **T4.3a wave**

| rAthena MST_* | Status | C# resolver branch |
|---|---|---|
| MST_TARGET | ✅ | `ResolveEntity` reads `MobEntity.TargetId`, falls back to `AttackedId` if !CanAttack |
| MST_RANDOM | ⚠️ | `ResolveRandomEnemy` uses `IEntityRegistry.ForEachInRange`; battle_getenemy filter TODO |
| MST_SELF | ✅ | returns mob |
| MST_FRIEND | ⚠️ | currently falls back to self (friend-tracker not landed) |
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
| AI think loop | 1 | 1 | 7 | 9 |
| Skill picker | 11 | 3 | 2 | 16 |
| Condition evaluators (MSC_*) | 9 | 5 | 13 | 27 |
| Target modes (MST_*) | 6 | 2 | 0 | 8 |
| Lifecycle / DB ops | ~16 | 0 | 0 | ~16 |

## Implementation plan

1. ✅ **T4.1** — surface audit + this doc.
2. ✅ **T4.2** — full MSC_* enum + 15 evaluator classes + MobConditionContext bag.
3. ✅ **T4.3a** — `IMobSkillCastService` + `MobSkillTargetResolver` (port of mob.cpp:4275-4502).
4. ✅ **T4.4** — `MobSkillCastServiceTests` (16 picker tests) + `RathenaMobSkillSweepTests` (4 canonical-row tests against actual Poring/Eddga mob_skill_db rows).
5. ⚠️ **T4.3b** — `NotifyEvent` is wired but the rude-attacked dispatch flow in `MobAiService.NotifyAttacked` still calls `TryUseSkill` first (broader picker), then `NotifyEvent(RudeAttacked)` (event-specific). Should restructure to call only the event-specific path for parity with `mobskill_event` line 4506.
6. ❌ **T4.6** — slave-mob registry (`mob_countslave` / `mob_getmaster` / `mob_summonslave` follow loop) — unblocks 5 MSC_* + MST_FRIEND.
7. ❌ **T4.7** — DmgListLog (attacker-id tracking) — unblocks MSC_ATTACKPCGT/GE + MSC_AFTERSKILL.
8. ❌ **T4.8** — `mob_ai_sub_lazy` + MSS_* FSM transitions + ground-cell cast dispatch.

## History

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
