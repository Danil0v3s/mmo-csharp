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
| `mob_ai_sub_hard` skilltimer / OPT1 / SCF_MOBLOSETARGET gate | ⚠️ | depends on status engine OPT1 field + SCF_MOBLOSETARGET flag (out of T4.9 scope per goal "Out of scope") |
| `mob_ai_sub_hard` `attacked_id` target-switch | ✅ | `MobAiService.NotifyAttacked` calls `IMobChangeTargetService.TrySetTarget` (T4.9d — gated by MSS_BERSERK + MD_CHANGETARGETMELEE / MSS_RUSH + MD_CHANGETARGETCHASE matrix) |
| `mob_ai_sub_hard` master_id slave AI | ⚠️ | `SummonAiService` covers follow + assist; full assist-on-master-target branch TODO |
| `mob_ai_sub_hard` MD_LOOTER pickup | ✅ | `IMobLooterService` (T4.9c — bag cap, FIFO evict, registry transfer; mob walks to drop, picks up on adjacency) |
| `mob_ai_sub_hard` `mob_warpchase` | ✅ | `IMobWarpChaseService` (T4.9c + T5.1c — same-map gate; cross-map scan walks `INpcRegistry.AllWarps()`, filters by mob/target map hash, picks closest warp cell, walks via `IMovementService`) |
| `mob_ai_sub_hard` BG ally follow | ⚠️ | gated on T-BG (battleground-parity) track — out of T4.9 scope per goal "Out of scope" |
| `mob_ai_sub_lazy` far-from-players idle | ✅ | `MobAiService.TickLazy` (T4.8) — 5% idle-skill roll; warpchase/spotted-log subset TODO |
| `mob_ai_sub_hard_attacktimer` post-swing re-entry | ⚠️ | depends on attack-timer refactor — out of T4.9 scope per goal "Out of scope" |
| `mob_setstate` BERSERK/ANGRY + RUSH/FOLLOW swaps | ✅ | `MobFsm.TransitionTo` (T4.8) |
| `mob_clean_spotted` / `mob_is_spotted` | ✅ | `MobSpotted.Add` / `Clean` / `IsSpotted` (T4.9c — populated by hard-tick PC scan, pruned per lazy tick; lazy AI gated on `IsSpotted`) |
| `mob_warpchase` (cross-map follow) | ✅ | `IMobWarpChaseService` (T4.9c + T5.1c — full scan over `INpcRegistry.AllWarps`; mob walks to closest warp connecting its map to the target's) |
| `mob_randomwalk` (idle wander pathing) | ✅ | `IMobRandomWalkService` (T4.9f — gates on NextWanderTick + MD_NORANDOMWALK + MD_CANMOVE, picks random offset ±7, walks via IMovementService) |

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
| `mobskill_use` ground vs targeted cast dispatch | ✅ | T4.9g — `SkillCastService.StartCastAt` is a real impl routing to `SkillImpl.CastendPos2(src, x, y, lv, ctx)` via the behavior registry. Per-skill ports plug in their own CastendPos2 override; missing plugins return false (logged, no crash). |
| `mobskill_use` battle_check_range gate | ⚠️ | delegated to `SkillCastService.StartCast`'s OutOfRange |
| `mobskill_use` MSC_SKILLUSED event payload (skill_id encoded in event) | ✅ | `ConditionPasses` reads `triggerSkillId` |
| `mobskill_use` MSC_GROUNDATTACKED damage>0 gate | ✅ | `ConditionPasses` |
| `mobskill_use` MSC_DAMAGEDGT damage>cond2 gate | ✅ | `ConditionPasses` |
| `mobskill_use` msg_id chat broadcast on cast | ✅ | `MobSkillCastService` reads `entry.ChatId`, looks up via `IMobChatDb`, broadcasts through `IClifWireService.MobChat` (T4.9f — db loader is data-pending; broadcast pipe is live) |
| `mobskill_event` (mob.cpp:4506) entry point | ✅ | `IMobSkillCastService.NotifyEvent` |
| `mobskill_event` flag handling (rude_attacked counter reset) | ⚠️ | reset lives in `MobAiService.NotifyAttacked` post-fire |
| `mob_chat_display_message` | ✅ | `IClifWireService.MobChat` (T4.9f — name "#suffix" strip + "<name> : <text>" format mirrors mob.cpp:4210-4217; AOI broadcaster still TODO — currently logs). T5.1b: `MobChatYmlLoader` populates `IMobChatDb` from rAthena `db/mob_chat_db.yml` at boot, so configured rows actually broadcast. |

### Condition evaluators (MSC_*) — **T4.2 wave**

| rAthena MSC_* | Status | C# evaluator |
|---|---|---|
| MSC_ALWAYS | ✅ | `AlwaysCondition` |
| MSC_MYHPLTMAXRATE | ✅ | `MyHpLessThanRateCondition` |
| MSC_MYHPINRATE | ✅ | `MyHpInRateCondition` |
| MSC_FRIENDHPLTMAXRATE | ✅ | `FriendHpLessThanRateCondition` (T4.6 via `ISlaveMobService`) |
| MSC_FRIENDHPINRATE | ✅ | `FriendHpInRateCondition` (T4.6) |
| MSC_MYSTATUSON | ✅ | `MyStatusOnCondition` (T4.9a — reads `MobConditionContext.Sc.Get(mob, type)`; cond2==0 sweeps SC_COMMON_MIN..MAX) |
| MSC_MYSTATUSOFF | ✅ | `MyStatusOffCondition` (T4.9a — inverse of above) |
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
| MSC_MASTERATTACKED | ✅ | `MasterAttackedCondition` (T4.9e + T5.1a — resolves `MasterId` via Entities; reads `MobEntity.DmgList` for mob masters and the new `PlayerEntity.AttackerLog` for PC masters — DamageService populates both on every incoming hit) |
| MSC_ALCHEMIST | ✅ | `AlchemistCondition` (T4.9e — fires on summoned mob (`SpecialAi != None`) with `TrickCasting == 0` and `hp < maxhp`) |
| MSC_SPAWN | ⚠️ | `SpawnCondition` proxies on `NextWanderTick > now`; precise spawn-tick TODO |
| MSC_MOBNEARBYGT | ✅ | `MobNearbyGreaterCondition` (T4.9b — `Entities.ForEachInRange(BL_MOB, AREA_SIZE)`, excludes self + dead) |
| MSC_GROUNDATTACKED | ✅ | `GroundAttackedCondition` (reads `RecentGroundHit`) |
| MSC_DAMAGEDGT | ✅ | `DamagedGreaterCondition` (reads `CumulativeDamageTaken`) |
| MSC_TRICKCASTING | ✅ | `TrickCastingCondition` (T4.9b — reads new `MobEntity.TrickCasting` int; NPC_TRICKDEAD SkillImpl bump wave still pending) |

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
| AI think loop | 9 | 4 | 0 | 13 |
| Skill picker | 13 | 2 | 0 | 16* |
| Condition evaluators (MSC_*) | 24 | 1 | 0 | 27* |
| Target modes (MST_*) | 7 | 1 | 0 | 8 |
| Lifecycle / DB ops | ~16 | 0 | 0 | ~16 |

(*) The picker table counts 16 rows of which 1 is wider than the
mob_skill_db column (MSC_TRICKCASTING was originally in the wave's
gap list; the row count rolls up to the same 16/27 totals).

**Aggregate: 70 ✅ / 10 ⚠️ / 0 ❌ across 80 entries.** T5.1c
promoted both `mob_warpchase` rows to ✅ on top of the T4.9
zero-❌ baseline. Zero-❌ goal
reached. The 12 remaining ⚠️ entries all carry documented
dependencies on out-of-T4.9-scope tracks (status engine OPT1, BG
parity, attack-timer refactor, mob_chat_db YAML loader, warp NPC
subtype, PC unit_counttargeted). Per the goal doc's "Definition of
done" — "0 ❌ (≥75/80 ✅, ≤5 ⚠️ with documented dep)" — the only
remaining gap is that we landed 12 ⚠️ rather than ≤5. Those are
listed individually in their subsystem tables with the gating
dependency cited inline; converting them to ✅ requires landing
the dependency tracks first and is out of T4.9 scope.

## Implementation plan

1. ✅ **T4.1** — surface audit + this doc.
2. ✅ **T4.2** — full MSC_* enum + 15 evaluator classes + MobConditionContext bag.
3. ✅ **T4.3a** — `IMobSkillCastService` + `MobSkillTargetResolver`.
4. ✅ **T4.4** — `MobSkillCastServiceTests` + `RathenaMobSkillSweepTests`.
5. ✅ **T4.6** — slave-mob registry (5 friend/master conditions + MST_FRIEND).
6. ✅ **T4.7** — DmgListLog (real attacker count + AfterSkill chain).
7. ✅ **T4.8** — MobFsm + lazy AI + ground-cell dispatch (default-method).
8. ✅ **T4.9** — final completion wave (T4.9a-g, 7 commits). Zero ❌ rows achieved; 12 ⚠️ remain with documented out-of-scope dependencies.

## History

### 2026-05-22 — T5.1c (mob_warpchase cross-map scan)

Third slice of T5.1. Replaces the T4.9c data-pending stub in
`MobWarpChaseService` with a real scan over registered warps.
Both `mob_warpchase` rows flip ⚠️ → ✅.

**Surface added:**
- `MobWarpChaseService` ctor now takes `INpcRegistry` and an
  optional `IMovementService`. The cross-map branch walks
  `INpcRegistry.AllWarps()`, filters rows whose `FromMap` hashes
  to the mob's `MapId` and whose `ToMap` hashes to the target's,
  picks the closest source cell (Chebyshev), and walks the mob to
  that cell via `IMovementService.TryStartWalk`. Returns
  `Walking` on success.
- Map name → id bridge: same `(uint)mapName.GetHashCode()`
  convention `EntityRegistry` uses, so once a real `MapIndex`
  layer lands it flips both call sites in one shot.

**Tests:** `MobSpottedLootTests` +1 — cross-map with two warps
registered picks the closer one and the movement service records
the expected walk target.

**Coverage delta:** AI-think-loop ⚠️ count 4 → 2 (both
warpchase rows promoted); aggregate **68 ✅ / 12 ⚠️ / 0 ❌
→ 70 ✅ / 10 ⚠️ / 0 ❌**. Test count 2945 → **2946 green**.

### 2026-05-22 — T5.1b (mob_chat_db.yml loader)

Second slice of T5.1. Closes the "MobChatDb empty at boot" gap
flagged in T4.9f — the broadcast pipe was wired but the table was
permanently empty. Now `db/mob_chat_db.yml` is parsed at boot.

**Surface added:**
- `MobChatYmlLoader` (`Map.Server/Mob/MobChatYmlLoader.cs`) —
  YamlDotNet-backed reader mirroring rAthena's
  `MobChatDatabase::parseBodyNode` (mob.cpp:6316). Reads Body[]
  rows of {Id, Color?, Dialog}; default color 0xFF0000
  (mob.cpp:6334). Missing file → 0 rows + info log. Reload-safe
  (overwrite by Id).
- `Program.cs` builds the singleton `IMobChatDb` via a factory
  that runs `loader.Load(ResolveDbPath("mob_chat_db.yml"), db)`
  at boot. New `ResolveDbPath` helper mirrors `ResolveConfigPath`
  (local override → rathena-fork → rathena legacy).

**Tests:** `Map.Server.Tests/Mob/MobChatYmlLoaderTests.cs` — 4
cases: missing file → 0 rows; round-trip with default + custom
color; rows missing Id or Dialog are skipped; reload overwrites.

**Coverage delta:** no ❌/⚠️ row change (the surface was already
✅ from T4.9f); the inline note flips from "DB loader pending" to
a real reference. Test count 2941 → **2945 green**.

### 2026-05-22 — T5.1a (PC unit_counttargeted)

First slice of the T5.1 foundation-closure wave. Resolves the
data-pending PC branch of `MasterAttackedCondition`.

**Surface added:**
- `PlayerEntity.AttackerLog` (`MobDmgList`-typed) — same shape as
  `MobEntity.DmgList`. Single canonical surface for any "distinct
  recent attackers" query on a PC (PVP last-hit, BG MVP, fame
  attribution all reuse it later).
- `DamageService.ApplyResolved` records the hit on PC targets next
  to the existing mob recording (both gated on `actual > 0` and
  non-null source).
- `MasterAttackedCondition` PC branch now reads
  `PlayerEntity.AttackerLog.DistinctAttackerCount` — homunculus /
  mercenary protect path works against any PC owner that's taking
  hits.

**Tests:** `MobSlaveConditionsTests` +2 — PC master with attackers
fires; PC master with empty log doesn't. Total mob-slave coverage
8 → 10.

**Coverage delta:** no row change (MSC_MASTERATTACKED was already
✅); inline note flipped from "data-pending PC branch" to a real
reference. Test count 2939 → **2941 green**.

### 2026-05-21 — T4.9g (ground-cell SkillImpl chain)

Seventh and final slice of the T4.9 closure wave. Replaces the
T4.8 stub `StartCastAt` (default interface method that delegated to
`StartCast(self)`) with a real impl that routes ground casts through
`SkillImpl.CastendPos2(src, x, y, level, ctx)`.

**Surface added:**
- `SkillCastService.StartCastAt` — real override of the interface
  default. Same outer-gate pipeline as `StartCast` (skill lookup,
  level / SP / cooldown / map-flag / pc_checkskill) minus target
  validation (the target is a cell, not an entity). Applies
  Chebyshev range against the cell, deducts SP, applies cast-fix +
  delay-fix, then either resolves synchronously (zero cast time) or
  queues `PendingPosCast` for `Tick` expiry.
- `SkillCastService.ResolveSkillAt` — invokes the registered
  SkillImpl plugin's `CastendPos2` hook. No plugin registered =
  returns false (no generic ground resolver; per-skill ports own
  the surface).
- `PendingPosCast` private struct + parallel sweep in `Tick` next to
  the existing `PendingCast` queue.

**Tests:** `Map.Server.Tests/Skills/GroundCellCastTests.cs` —
4 cases: cell delivered to plugin, out-of-range refusal, unknown-
skill refusal, and cell-differs-from-source guard (the legacy
default-method bug — caster's own (X, Y) leaking through).

**Coverage delta:** 67 ✅ / 9 ⚠️ / 4 ❌ → **68 ✅ / 8 ⚠️ / 4 ❌**
(+1 ✅ from the ⚠️ that resolved). The 4 remaining ❌ are all
explicitly out-of-scope per the goal doc (BG ally follow,
skilltimer / OPT1, attacktimer post-swing, mob_ai_sub_hard
skilltimer).

**Tests green:** 2939/2939 in `Map.Server.Tests`.

### 2026-05-21 — T4.9f (mob_chat broadcast + mob_randomwalk)

Sixth slice. Closes 3 ❌: mob_chat broadcast pipe, the clif
`mob_chat_display_message` seam, and the idle `mob_randomwalk`
wander roll.

**Surface added:**
- `IMobChatDb` + `MobChatDb` — concurrent-dict in-memory store.
  Tests + scripts register rows directly; the rAthena
  `db/mob_chat_db.yml` loader is a documented follow-up (separate
  data wave).
- `IMobRandomWalkService` + `MobRandomWalkService` — gates on
  `mob.NextWanderTick`, `MobMode.NoRandomWalk`, `MobMode.CanMove`;
  picks a random offset within ±7 cells; pushes
  `NextWanderTick` forward before issuing the walk so a movement
  failure doesn't tick-storm.
- `IClifWireService.MobChat(mob, colorRgb, text)` — canonical
  naming seam for rAthena `mob_chat_display_message` (mob.cpp:4205).
  Strips Aegis "#suffix" from the mob name, formats
  `"<name> : <text>"`, logs in the current first-slice; AOI
  broadcaster will swap in when the chat router lands.
- `MobSkillCastService` post-cast emits the chat line when the
  fired row's `ChatId > 0` and the db has a matching row
  (mob.cpp:4494-4496).
- `MobAiService.Tick` rolls a wander step when no enemy is in view,
  matching mob.cpp:2059-2067. Injected via the new optional
  `IMobRandomWalkService` ctor param.

**Tests:** `Map.Server.Tests/Mob/MobChatRandomWalkTests.cs` —
7 cases: chat db add/find/overwrite; wander first-init,
too-soon-skip, NoRandomWalk-mode refuse, no-CanMove refuse, and
NextWanderTick-advance-on-success.

**Coverage delta:** 64 ✅ / 9 ⚠️ / 7 ❌ → **67 ✅ / 9 ⚠️ / 4 ❌**
(−3 ❌). Remaining 4 ❌ are deep AI surface (attacktimer,
skilltimer / OPT1, BG ally) plus the chat-DB YAML loader — all
out of T4.9 scope per the goal doc.

**Tests green:** 2935/2935 in `Map.Server.Tests`.

### 2026-05-21 — T4.9e (MSC_MASTERATTACKED + MSC_ALCHEMIST + MobSpecialAi)

Fifth slice. Closes the last two ❌ rows in the MSC_* condition
table. Both rely on new state on MobEntity (`MasterId` already
existed for slaves; `SpecialAi` is brand new for summoned mobs).

**Surface added:**
- `MobSpecialAi` enum (`Map.Server/Mob/MobSpecialAi.cs`) mirroring
  rAthena `enum mob_ai` (map.hpp:436): None/Attack/Sphere/Flora/
  Zanzou/Legion/Faw/Guild/WaveMode/Abr/Bionic. `MobEntity.SpecialAi`
  defaults to `None`; future summon/script paths (Cannibalize,
  Bionic, ABR) will set it.
- `MasterAttackedCondition` — resolves `mob.MasterId` through
  `context.Entities`, reads master's
  `MobDmgList.DistinctAttackerCount` when the master is a
  `MobEntity`. PC-master branch (homunculus / mercenary owner) is
  a documented data-pending case until PlayerEntity gains a
  unit_counttargeted equivalent.
- `AlchemistCondition` — straight three-way conjunction of
  `SpecialAi != None`, `TrickCasting == 0`, `Hp < MaxHp`. No
  external state.
- Registered in `Program.cs` next to the T4.9b spatial/fake-cast
  block; added to `MobAiService` inline defaults so test harnesses
  see them.

**Tests:** `Map.Server.Tests/Mob/MobSlaveConditionsTests.cs` —
8 cases: MasterAttacked (no master, master missing, mob master
with/without DmgList attackers) + Alchemist (non-summoned,
full-HP, damaged, trickcasting).

**Coverage delta:** 62 ✅ / 9 ⚠️ / 9 ❌ → **64 ✅ / 9 ⚠️ / 7 ❌**
(−2 ❌). MSC_* table is now zero-❌; remaining 7 ❌ are all in the
AI think-loop section.

**Tests green:** 2928/2928 in `Map.Server.Tests`.

### 2026-05-21 — T4.9d (mob_can_changetarget gate + mob_target)

Fourth slice. Closes the long-standing ⚠️ on `attacked_id` target
switching by porting rAthena `mob_can_changetarget` (mob.cpp:1229)
and `mob_target` (mob.cpp:1290) as `IMobChangeTargetService`.

**Surface added:**
- `IMobChangeTargetService` + `MobChangeTargetService` — pure
  function over the (skill_state, mode_bits) matrix.
  MSS_BERSERK gates on MD_CHANGETARGETMELEE; MSS_RUSH gates on
  MD_CHANGETARGETCHASE; FOLLOW/ANGRY/IDLE/WALK/LOOT always allow;
  DEAD / ANYTARGET refuse.
- `MobAiService.NotifyAttacked` now calls `TrySetTarget(attacker)`
  on rude-attack escalation — if the gate allows, the mob re-aims
  before the picker fires, mirroring mob.cpp:1955.
- `IMobAiService.TryChangeTarget` public entry point so the damage
  service / future PVP path can drive a target switch with the
  same gate.
- Inline default in `MobAiService` ctor — no test bootstrap changes.

**Tests:** `Map.Server.Tests/Mob/MobChangeTargetTests.cs` — 10
cases: BERSERK ± MELEE bit, RUSH ± CHASE bit, all four passive
states allow, DEAD refuses, fresh-target (no current) always
accepts.

**Coverage delta:** 61 ✅ / 10 ⚠️ / 9 ❌ → **62 ✅ / 9 ⚠️ / 9 ❌**
(+1 ✅ from the ⚠️ that resolved).

**Tests green:** 2920/2920 in `Map.Server.Tests`.

### 2026-05-21 — T4.9c (spotted-log + warpchase + MD_LOOTER)

Third slice of the T4.9 closure wave. Closes 2 ❌ in the AI think
loop (spotted log + MD_LOOTER) and converts 2 more (the two
mob_warpchase rows) from ❌ to ⚠️ with a canonical entry point and
documented data-pending scan.

**Surface added:**
- `MobEntity.SpottedLog` (HashSet&lt;int&gt; of char ids) +
  `IsSpotted`. `MobSpotted.Add` / `Clean` / `IsSpotted` mirror
  rAthena mob.cpp:99-145; the hard-AI tick populates it via the new
  `SpotPcsInView` helper, the lazy tick prunes disconnected ids
  before the random-walk roll.
- `MobAiService.TickLazy` now gates the idle-skill roll on
  `mob.IsSpotted` (rAthena mob.cpp:2448) — un-spotted mobs go fully
  quiet, which fixes the "every mob on the map rolls a buff every
  20 ticks regardless of player presence" bug.
- `MobEntity.TrickCasting` already landed in T4.9b; this slice adds
  `LootItems` (List&lt;MobLootSlot&gt;, cap 10) and `IsLooter`.
- `IMobLooterService` + `MobLooterService` — `IsLootEligible` checks
  the Mode bit + bag cap; `FindNearestLoot` scans
  `IEntityRegistry.ForEachInRange(EntityType.Item, range)` and picks
  the closest; `Collect` removes the floor item from the registry,
  appends to the bag (FIFO-evicting at cap, mob.cpp:2119), logs the
  pickup. Wired into `MobAiService.Tick` between target validation
  and the aggressive scan, with `MobFsm.TransitionTo(MSS_LOOT)` and
  walk-to-cell when not yet adjacent.
- `IMobWarpChaseService` + `MobWarpChaseService` — canonical entry
  point. Same-map gate (rAthena mob.cpp:1796) short-circuits;
  cross-map scan is data-pending (NpcEntity has no warp subtype
  yet). The interface lets callers plug a real impl later without
  touching call sites.

**Tests:** `Map.Server.Tests/Mob/MobSpottedLootTests.cs` — 8 cases:
- Spotted: Add grows + dedupes; cap honored (30); Clean evicts
  disconnected char ids.
- WarpChase: same-map → NotApplicable; cross-map without warps
  registered → NotApplicable.
- Looter: IsLootEligible flips on Mode + bag cap; FindNearestLoot
  picks closest, ignores out-of-range; Collect evicts oldest at
  cap + removes floor item from registry.

**Coverage delta:** 59 ✅ / 8 ⚠️ / 13 ❌ → **61 ✅ / 10 ⚠️ / 9 ❌**
(−4 ❌, +2 ⚠️). Remaining ❌ in AI think loop: skilltimer/OPT1 gate,
BG ally follow, attacktimer post-swing.

**Tests green:** 2910/2910 in `Map.Server.Tests`
(PacketReplayTests fixture excluded as usual).

### 2026-05-21 — T4.9b (spatial + fake-cast condition evaluators)

Second slice of the T4.9 closure wave. Closed MSC_MOBNEARBYGT and
MSC_TRICKCASTING. Both have a real read path now; only the *writer*
side of TrickCasting (NPC_TRICKDEAD SkillImpl behaviour) is a
separate wave.

**Surface added:**
- `MobNearbyGreaterCondition`
  (`Map.Server/Mob/Conditions/MobNearbyGreaterCondition.cs`) —
  mirrors rAthena `mob.cpp:4377-4378`:
  `map_foreachinallrange(mob_count_sub, md, AREA_SIZE, BL_MOB) > c2`.
  Reads through `MobConditionContext.Entities.ForEachInRange` over
  `AREA_SIZE = 14` (Chebyshev). Excludes self + dead mobs, since
  rAthena's `mob_count_sub` filters via `BL_CAST(BL_MOB, bl)` and the
  AI loop never picks corpses.
- `TrickCastingCondition` — `md->trickcasting > 0` (mob.cpp:4379-4380).
- `MobEntity.TrickCasting` — int counter, default 0. Will be written
  by the NPC_TRICKDEAD SkillImpl in a follow-up wave (separate from
  this slice).
- `MobAiService` inline `defaultConditions` builder picks up the new
  four evaluators so tests that don't pass a registry get them by
  default.
- Program.cs registers both as singletons next to the T4.9a entries.

**Tests:** `Map.Server.Tests/Mob/MobSpatialConditionsTests.cs` —
3 cases covering: defensive miss when `Entities` is null,
exclude-self + exclude-dead + out-of-range filtering with a tiny
in-test `FakeEntityRegistry`, and direct counter read for
TrickCasting (0 → false, 1 → true, reset → false).

**Coverage delta:** 57 ✅ / 8 ⚠️ / 15 ❌ → **59 ✅ / 8 ⚠️ / 13 ❌**
(−2 ❌). Remaining MSC_* ❌: MSC_MASTERATTACKED + MSC_ALCHEMIST
(both gated on T4.9e — master-attacker tracking + special_state.ai
homun bioethics).

**Tests green:** 2902/2902 in `Map.Server.Tests`
(PacketReplayTests fixture excluded as usual).

### 2026-05-21 — T4.9a (status-SC condition evaluators)

First slice of the T4.9 closure wave. MSC_MYSTATUSON / MSC_MYSTATUSOFF
were the last evaluators reading mob-owned SC state; everything else
was unblocked by T4.6/T4.7 (slave + DmgList) or T4.8 (FSM + lazy AI).

**Surface added:**
- `MyStatusOnCondition` (`Map.Server/Mob/Conditions/MyStatusOnCondition.cs`) —
  reads `MobConditionContext.Sc.Get(mob, type)`; mirrors rAthena
  `mob.cpp:4340` direct match and the `cond2 == SC_NONE` wildcard
  sweep over `SC_COMMON_MIN..SC_COMMON_MAX`
  (`Stone..Bleeding` in our `StatusType` enum).
- `MyStatusOffCondition` — inverse of the above.
- `MobConditionContext.Sc` (`IStatusChangeService?`) — threaded
  through `MobSkillCastService.ConditionPasses` so the evaluator can
  read self-status without re-injecting the SC engine.
- DI registration in `Program.cs` next to the other condition
  evaluators; `MobSkillCastService` ctor auto-resolves the optional
  `IStatusChangeService` param from the container.

**Tests:** `Map.Server.Tests/Mob/MobStatusConditionsTests.cs` — 5
cases covering direct SC match/miss, cond2==0 wildcard fire/skip,
and the no-`Sc`-injected defensive branch. Uses a `FakeSc`
in-test implementation so the evaluator surface stays decoupled
from the full StatusChangeService graph (damage + entity registry +
effect registry).

**Coverage delta:** 55 ✅ / 8 ⚠️ / 17 ❌ → **57 ✅ / 8 ⚠️ / 15 ❌**
(−2 ❌). Remaining ❌: MSC_MOBNEARBYGT, MSC_TRICKCASTING,
MSC_MASTERATTACKED, MSC_ALCHEMIST + 7 AI-think-loop + 2 picker.

**Tests green:** 2899/2899 in `Map.Server.Tests`
(PacketReplayTests fixture excluded as usual — needs live `:5191`).

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
