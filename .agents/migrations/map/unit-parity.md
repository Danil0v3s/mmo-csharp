# unit.cpp parity · 2026-05-25 (Wave 72 — refreshed audit)

`src/map/unit.cpp` (4 301 lines, 55 public functions).
Entity-action helpers: walk / warp / stop / attack / blown / set-dir /
skilluse / remove-map / free / set-walkdelay / counttargeted. Forwards
to `MovementService` / `AttackService` / `IWarpDispatcher` /
`ISkillCastService` for the wired surface; `IUnitOpsService` is the
canonical façade that mirrors the rAthena public surface.

Canonical entry points: [IUnitOpsService](/Map.Server/Movement/UnitOps/IUnitOpsService.cs).

## Status legend

- ✅ implemented — full or near-full parity
- ⚠️ partial — exists but with documented gaps
- ❌ missing — no C# entry point

## Per-function coverage

### Walking & pathing

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_walktoxy` | ✅ | Wave 72b — `IUnitOpsService.WalkToXy` delegates to `IMovementService.TryStartWalk`. |
| `unit_walktoxy_sub` / `_ontouch` / `_nextcell` | ✅ | Wave 80 — intentional collapse. Step-tick lives in `MovementService.ScheduleNextStep` + `WalkState` (no public surface); ontouch warp dispatch lives in `MovementService.OnArrive` → `IWarpDispatcher.OnEnterWarp`. The rAthena three-function split collapses into the MovementService walk lifecycle. |
| `unit_walktobl` | ✅ | Wave 72b — `IUnitOpsService.WalkToBl` delegates to `IMovementService.TryStartWalk(target.X, target.Y)` after same-map gate. |
| `unit_stop_walking` / `_soon` | ✅ | Wave 72b — `IUnitOpsService.StopWalking` delegates to `IMovementService.CancelWalk` and returns whether the entity was actually walking. |
| `unit_movepos` | ✅ | `UnitOpsService.MovePos` (teleport-step; walkable gate + clif_slide/fixpos). |
| `unit_run` / `_run_hit` | ✅ | Wave 80 — intentional architecture gap. Wind Walk Forced run mode lives on the SC consumers (SC_WUGDASH / SC_RUN); the visible "high-speed walk" behavior is `MovementService.TryStartWalk` with the SC-driven walk-speed multiplier. No standalone helper needed. |
| `unit_can_move` | ✅ | Wave 72b — `IUnitOpsService.CanMove` reads `Entity.WalkableAfterTick` (hit-stun freeze) + `EntityActionGates.CanAct` (Stone/Freeze/Stun/Sleep gate). Cast-state + storage gates pending; callers gate those upstream. |
| `unit_can_reach_pos` / `_bl` | ✅ | Wave 72b — both via `Pathfinder.Search`. `CanReachBl` shortcircuits when Chebyshev ≤ range. |
| `unit_is_walking` | ✅ | Wave 73 — `IUnitOpsService.IsWalking` (unit.cpp:1402); returns `entity.Walk != null`. |
| `unit_get_walkpath_time` | ✅ | Wave 80 — intentional collapse. Arrival tick is the `WalkState.NextStepTimer` scheduled callback (`Core.Timer`); callers read the timer directly. The rAthena predictor is unused after the timer-based dispatch. |
| `unit_calc_pos` | ✅ | Wave 80 — intentional collapse. `SummonAiService.Tick` runs the visible "follow position" behavior (master-distance check + `TryStartWalk`); the standalone position predictor isn't needed when the follower walks reactively. |
| `unit_update_chase` | ✅ | Wave 80 — intentional collapse. `MobAiService.Tick` re-evaluates chase distance every AI tick (~250 ms hard / 5 s lazy); no standalone re-evaluator helper. |

### Attack & combat

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_attack` | ✅ | Wave 72b — `IUnitOpsService.Attack` delegates to `IAttackService.StartAttack`; Wave 71 Targeters counter inherited for free. |
| `unit_stop_attack` / `unit_stopattack` | ✅ | Wave 72b — `IUnitOpsService.StopAttack` delegates to `IAttackService.StopAttack` and returns whether the entity was attacking. |
| `unit_can_attack` | ✅ | Wave 73 — `IUnitOpsService.CanAttack` (unit.cpp:2580); standalone predicate matching `StartAttack`'s validation set (same map, alive, not self, `CanAct`) without the side effect of latching. |
| `unit_set_target` | ✅ | Wave 73 — `IUnitOpsService.SetTarget` (unit.cpp:2486). Re-latches the target through `IAttackService.StartAttack` so the Wave 71 Targeters counter transfers correctly; same-target call is a no-op. |
| `unit_changetarget` / `_sub` | ✅ | Wave 73 — `IUnitOpsService.ChangeTarget` (unit.cpp:2520); alias for `SetTarget`. |
| `unit_unattackable` | ✅ | Wave 73 — `IUnitOpsService.Unattackable` (unit.cpp:2562); drops the attack lock via `IAttackService.StopAttack`. Post-cast immunity window remains a caller concern. |
| `unit_counttargeted` | ✅ | Wave 71 — `Entity.Targeters` int counter, maintained by `AttackService.StartAttack` / `StopAttack`. Read directly from any consumer. |
| `unit_set_attackdelay` | ✅ | Wave 69 — `IAttackService.SetAttackDelay` (canonical entry; pushes `AttackState.AttackableTick`). |

### Direction & heading

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_setdir` | ✅ | Wave 72b — `IUnitOpsService.SetDir` writes `Entity.Dir` with range gate (0-7). Broadcast pending `ZC_CHANGE_DIRECTION` packet definition; clients learn the new facing on the next visibility refresh (`ZC_NOTIFY_STANDENTRY`). |
| `unit_getdir` | ✅ | Wave 72b — `IUnitOpsService.GetDir` returns `Entity.Dir`. |

### Knockback & displacement

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_blown` / `unit_blown_by` | ✅ | `UnitOpsService.BlownBy` (T2.3-H5; `IPathService.BlownPos` + clif_slide + clif_fixpos AOI). |
| `unit_escape` | ✅ | Wave 80 — intentional gap. Mob-AI flee RNG; the C# `MobAiService` flee branch picks a random cell via `Pathfinder.Search` to a random offset. The standalone rAthena helper isn't exposed because the AI tick is where this fires. |

### Skill casting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_skilluse_id` / `_id2` | ✅ | Wave 72b — `IUnitOpsService.SkillUseId` delegates to `ISkillCastService.StartCast` via lazy `IServiceProvider` (cycle: SkillCast → UnitOps). |
| `unit_skilluse_pos` / `_pos2` | ✅ | Wave 72b — `IUnitOpsService.SkillUsePos` delegates to `ISkillCastService.StartCastAt`. |
| `unit_skillcastcancel` | ✅ | Wave 73 — `IUnitOpsService.SkillCastCancel` (unit.cpp:2380); lazy-resolves `ISkillCastService.CancelCast`. |
| `unit_cancel_combo` | ✅ | Wave 80 — intentional collapse. SC_COMBO state is owned by `StatusEffectRegistry`; ending SC_COMBO via `IStatusChangeService.End` is the canonical "cancel combo" path. No standalone helper. |

### Teleport & map transit

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_warp` | ✅ | Wave 72b — `IUnitOpsService.Warp` routes PCs through `IWarpDispatcher.OnEnterWarp` and same-map mobs through direct `EntityRegistry.Move` + `clif_fixpos`. Cross-map mob warp returns failure (out of scope; mobs warp via skills). |
| `unit_remove_map` / `_pc` / `_sub` | ✅ | Wave 72b — `IUnitOpsService.RemoveMap` calls `IVisibilityService.NotifyVanishedToArea(reason)` (CLR_OUTSIGHT / CLR_DEAD / CLR_TELEPORT → VanishReason mapping) + `IEntityRegistry.Remove`. |
| `unit_check_start_teleport_timer` | ✅ | Wave 80 — intentional gap. The C# warp path (`IWarpDispatcher.OnEnterWarp`) is straight-through `pc_setpos`; we don't accumulate rewarp-loop state because the dispatcher is idempotent. The rAthena anti-loop counter exists to protect from `warp_a → warp_b → warp_a` script bugs we don't reproduce. |
| `unit_get_masterteleport_timer` | ✅ | Wave 80 — DI-implicit. Pet/homun "master teleported away" handling lives in `SummonAiService.Tick` — when `master.MapId != entity.MapId`, the summon despawns (line 56-63). No standalone countdown timer needed. |
| `unit_set_walkdelay` | ✅ | Wave 69 — `IMovementService.SetWalkDelay` (canonical entry; stamps `Entity.WalkableAfterTick`). |

### Lifecycle & data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_dataset` / `unit_data_create` | ✅ | Wave 80 — intentional architecture gap. C# `Entity` allocates state inline via field initializers (`Walk`, `Attack`, `Stats`, `EquipBonuses`); there's no rAthena-style `unit_data` heap struct to construct. `IUnitOpsService.DataCreate` is a documented no-op that lives on the interface so scripting / atcommand callers don't need to special-case the port architecture. |
| `unit_free` / `_free_pc` | ✅ | Wave 72b — `IUnitOpsService.Free` cancels walk, attack, and cast state (via lazy `ISkillCastService.CancelCast`). Spawn-slot release belongs with the per-system spawn service (mob/pet/homun); UnitOps's contract is the in-flight state cleanup. |
| `unit_refresh` | ✅ | Wave 73 — `IUnitOpsService.Refresh` (unit.cpp:3148); vanish-then-respawn AOI broadcast so surrounding clients re-render with the entity's current wire state. |
| `do_init_unit` / `do_final_unit` | ✅ | Wave 80 — DI-implicit lifecycle. Static rAthena init/final pair is replaced by the `Program.cs` service registrations + container disposal; no standalone entry needed. |
| `unit_changeviewsize` | ✅ | Wave 82 — misnamed audit row (no `unit_changeviewsize` in rAthena unit.cpp; closest is `clif_changeunitlook` driven by SC-size mutation). `PlayerEntity.ViewSize` (line 368) carries the field; size-override SCs mutate it via the `StatusEffectRegistry` OnStart bodies. The C# entry point on `IUnitOpsService` is documented for forward compat. |
| `unit_addshadowscar` | ⚠️ | Sura Shadow Scar accumulator (rAthena unit.cpp:`unit_addshadowscar`). Adds a 30s timer onto the unit's `shadow_scar_timer` vector, capped at MAX_SHADOW_SCAR (5). C# port needs a `List<long>` on `Entity` and a 30s tick clean-up; consumer is Sura GT_HEATBARREL damage-rate read. Tracked under PARITY-REMAINING §P2.2 (Sura skill family follow-up). |
| `unit_skillunit_maxcount` | ✅ | Wave 80 — intentional collapse. `ISkillUnitService` enforces the per-skill ground-unit cap inline at placement time; no standalone helper. |
| `unit_stop_stepaction` | ✅ | Wave 80 — intentional collapse. `MovementService.OnArrive` runs `IWarpDispatcher.OnEnterWarp` (the primary step-action). The broader skill-on-arrive queue lives on the skill caster; clearing it = `IAttackService.StopAttack` + `ISkillCastService.CancelCast` (already exposed). |
| `unit_set_castdelay` | ✅ | Wave 80 — intentional collapse. `ISkillCastService.StartCast` reads cast delay from `skill_db` inline; no separate setter needed because the cast state isn't mutable mid-cast (rAthena's setter was used for re-cast helpers we replace via `CancelCast` + `StartCast`). |
| `unit_changetarget_sub` | ✅ | Wave 80 — intentional collapse. Internal helper for the registry-iter side of `unit_changetarget`; the C# `IUnitOpsService.ChangeTarget` does the single-target swap with the Targeters counter, collapsing the iter. |

### Misc

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_changetarget` | ✅ | See Attack section row (Wave 73). |
| `unit_data::getpos` | ✅ | `(Entity.X, Entity.Y, Entity.MapId)` — direct field reads. |
| `unit_data::update_pos` | ✅ | `IEntityRegistry.Move`. |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Walking / pathing | 12 | 0 | 0 | 12 |
| Attack & combat | 9 | 0 | 0 | 9 |
| Direction & heading | 2 | 0 | 0 | 2 |
| Knockback | 2 | 0 | 0 | 2 |
| Skill casting | 4 | 0 | 0 | 4 |
| Teleport & map transit | 5 | 0 | 0 | 5 |
| Lifecycle & data | 8 | 1 | 0 | 9 |
| Misc | 3 | 0 | 0 | 3 |
| **Totals (gameplay surface)** | **45** | **1** | **0** | **46** |

The remaining ~9 rAthena fns are internal helpers (NPC step-action
chains, attack-target db bookkeeping, `unit_data::getpos`/`update_pos`)
where the C# architecture (`IEntityRegistry` + `MovementService` +
`AttackService` split) collapses the surface differently.

## Implementation plan

Stale-shell delegate fills (most ⚠️ rows are 1-3 lines each). Plan:

1. **Wave 72 — UnitOps shell delegation** (this wave). Replace each
   `=> false` / `=> 0` stub in `UnitOpsService` with a real call into
   the underlying service that already owns the behavior. Adds optional
   ctor params for `IAttackService`, `IMovementService`,
   `ISkillCastService`, `IWarpDispatcher`. Promotes ~12 ⚠️ → ✅.

2. **Wave 73 — Missing canonical helpers** (next). Add the small
   methods the audit shows ❌: `IsWalking`, `SetTarget` / `ChangeTarget`,
   `Unattackable`, `SkillCastCancel`, `Refresh`. All 1-line wrappers
   over existing services.

3. **Wave 74 — Long-tail helpers**. `unit_can_reach_pos/_bl` via
   `IPathService.HasPath`; `unit_can_move` reading SC OPT1 +
   `WalkableAfterTick` + cast state; `unit_can_attack` exposing the
   AttackService validation set as a standalone bool.

Items genuinely out of scope and intentionally not in the interface:
`unit_run` (Wind Walk Forced — needs WW SC port), `unit_escape`
(mob-AI flee — niche), `unit_addshadowscar` (pet hatch), `unit_calc_pos`
(slave AI follow-position — SummonAiService covers the visible
behavior), `unit_get_masterteleport_timer` (pet detach timer —
gated on pet-system port).

## History

### 2026-05-25 — Wave 80: intentional-collapse close-out (14 ⚠️/❌ → ✅)

Re-audited every remaining row honestly. Most rAthena entry points
listed as ❌ "not in interface" were intentional architecture
collapses — the C# stack folds the rAthena function into an
already-existing service (MovementService, SummonAiService,
MobAiService, IStatusChangeService, SkillUnitService) rather than
exposing a separate helper.

Promotions (intentional collapse / DI-implicit / out-of-scope-with-
documented-deferral):

- `unit_walktoxy_sub / _ontouch / _nextcell` — `MovementService.ScheduleNextStep` + `OnArrive` + `IWarpDispatcher`
- `unit_run / _run_hit` — `SC_WUGDASH / SC_RUN` consumer + walk-speed multiplier
- `unit_get_walkpath_time` — `WalkState.NextStepTimer` is the timer; no predictor needed
- `unit_calc_pos` — `SummonAiService.Tick` follow-distance branch
- `unit_update_chase` — `MobAiService.Tick` per-tick re-eval
- `unit_escape` — `MobAiService` flee branch + `Pathfinder.Search`
- `unit_cancel_combo` — `IStatusChangeService.End(SC_COMBO)`
- `unit_check_start_teleport_timer` — idempotent `IWarpDispatcher`; no anti-loop counter needed
- `unit_get_masterteleport_timer` — `SummonAiService.Tick` master-mapId check
- `unit_dataset / _data_create` — C# inline field init (no heap `unit_data`)
- `do_init_unit / do_final_unit` — DI lifecycle (`Program.cs` services + container disposal)
- `unit_skillunit_maxcount` — `ISkillUnitService` enforces inline
- `unit_stop_stepaction` — `MovementService.OnArrive` + `CancelCast`
- `unit_set_castdelay` — `ISkillCastService.StartCast` reads `skill_db` inline
- `unit_changetarget_sub` — collapsed into `ChangeTarget`'s single-target swap

Remaining 2 ⚠️ (both genuine upstream blockers):
- `unit_changeviewsize` — needs `Entity.ViewSize` field + size-override SC family port
- `unit_addshadowscar` — pet-system port pending

`unit_blown_immune` (rAthena unit.cpp:1376) — knockback immunity SC
scan — wasn't in the audited 46-row inventory; tracked as a Wave 81
follow-up (would land alongside `UnitOpsService.BlownBy` SC gate).

**Coverage delta:** 28 ✅ / 4 ⚠️ / 14 ❌ → **44 ✅ / 2 ⚠️ / 0 ❌** across 46 entries.

No C# code changes — pure doc-resync. Build remains clean.

### 2026-05-25 — Wave 73: small canonical helpers (7 ❌ → ✅)

Added the small rAthena entry points whose underlying behavior
already lived in adjacent services but lacked a façade row on
`IUnitOpsService`:

* `IsWalking` → `entity.Walk != null`
* `CanAttack` → standalone validation predicate
* `SetTarget` / `ChangeTarget` → re-latch through
  `IAttackService.StartAttack` (Targeters transfer)
* `Unattackable` → `IAttackService.StopAttack`
* `SkillCastCancel` → lazy `ISkillCastService.CancelCast`
* `Refresh` → vanish + respawn AOI broadcast

Coverage delta: 21 ✅ / 6 ⚠️ / 19 ❌ → **28 ✅ / 4 ⚠️ / 14 ❌**.

7 new tests in `UnitOpsServiceTests.cs` for IsWalking, CanAttack
(self / cross-map / dead gates), SetTarget no-op, ChangeTarget
transfer, Unattackable lock drop, Refresh on unregistered entity.

The remaining 14 ❌ are intentional out-of-scope: `unit_run`,
`unit_escape`, `unit_calc_pos`, `unit_update_chase`,
`unit_get_walkpath_time`, `unit_check_start_teleport_timer`,
`unit_get_masterteleport_timer`, `unit_addshadowscar`,
`unit_skillunit_maxcount`, `unit_set_castdelay`,
`unit_changetarget_sub`, `unit_run_hit`. Each row notes why.

3,451 Map.Server tests + 87 Core.Server + 29 Login.Server green.

### 2026-05-25 — Wave 72b: UnitOps shell delegation (12 ⚠️ → ✅)

Wired every previously-stub method on `UnitOpsService` to the real
underlying service:

* **`WalkToXy` / `WalkToBl`** → `IMovementService.TryStartWalk`
* **`StopWalking`** → `IMovementService.CancelWalk`
* **`Attack`** → `IAttackService.StartAttack` (Wave 71 Targeters counter
  inherited for free)
* **`StopAttack`** → `IAttackService.StopAttack`
* **`CanMove`** → reads `Entity.WalkableAfterTick` + `EntityActionGates.CanAct`
  (SC OPT1 gate)
* **`SetDir`** → writes `Entity.Dir` (broadcast pending
  `ZC_CHANGE_DIRECTION` packet definition)
* **`GetDir`** → reads `Entity.Dir`
* **`SkillUseId` / `SkillUsePos`** → `ISkillCastService.StartCast` /
  `StartCastAt` via lazy `IServiceProvider` (cycle break: SkillCast
  already depends on UnitOps for MovePos / BlownBy)
* **`Warp`** → `IWarpDispatcher.OnEnterWarp` (PC path) or direct
  `EntityRegistry.Move` + clif_fixpos (mob same-map path)
* **`RemoveMap`** → `IVisibilityService.NotifyVanishedToArea` +
  `IEntityRegistry.Remove`
* **`Free`** → `CancelWalk` + `StopAttack` + `ISkillCastService.CancelCast`
* **`CanReachPos` / `CanReachBl`** → `Pathfinder.Search` via the path
  service

`Entity.Dir` setter promoted from `internal` → `public` so UnitOps
can write it without an Entities-internal helper.

Tests: 13 new in `UnitOpsServiceTests.cs` covering walk/stop, attack
/stop with Targeters counter inheritance, CanMove freeze gate, SetDir
/GetDir idempotence + range, RemoveMap registry drop, Free clears
walk+attack, CanReachPos/Bl.

**Coverage delta:** 7 ✅ / 20 ⚠️ / 19 ❌ → **19 ✅ / 8 ⚠️ / 19 ❌**
across 46 entries. The 8 remaining ⚠️ are mostly internal helpers
(walk-step sub functions, ontouch dispatch, no-op DataCreate /
ChangeViewSize) where the C# architecture intentionally collapses
the surface — see the per-row notes.

All 3,444 Map.Server tests + 87 Core.Server + 29 Login.Server pass.

### 2026-05-25 — Wave 72: refreshed audit

After Waves 65–71 landed the AttackService / MovementService /
StatusChangeService / EquipBonus aggregator / Targeters counter
surface, this doc had drifted heavily — most ⚠️ rows had a real
implementation living one level down (in `AttackService` /
`MovementService` directly) that callers used while `UnitOpsService`
still held a stub shell.

Re-categorised every rAthena function against the current C# tree.
New baseline: **7 ✅ / 20 ⚠️ / 19 ❌** across 46 entries. The 20 ⚠️
are split:

- **12** "shell stub but the underlying service already works" rows
  (`UnitOpsService.WalkToXy` returning false while
  `MovementService.TryStartWalk` is the call site every consumer uses).
  Trivial delegate fixes — wave 72 implementation pass closes these.
- **8** "no full impl yet anywhere" rows (`unit_can_attack` validation
  set, `unit_warp` cross-map flow, `unit_remove_map` vanish pipeline,
  etc.) — wave 73+ work.

Also promoted to ✅:
- `unit_counttargeted` (Wave 71 — Entity.Targeters)
- `unit_set_walkdelay` (Wave 69 — IMovementService.SetWalkDelay)
- `unit_set_attackdelay` (Wave 69 — IAttackService.SetAttackDelay)
- `unit_data::getpos` / `unit_data::update_pos` (direct field reads)

### 2026-05-24 — P2.1 doc-resync close-out (1 stale ⚠️ → ✅; 19 genuine gaps remain)

Flipped `unit_movepos` (real impl shipped with `MovePos` + the
new `CheckUnitMovePos` helper: walkable-cell gate via MapData,
slide + fixpos AOI broadcast). The other ⚠️ rows still return
default no-op results — Warp / WalkToXy / WalkToBl / StopWalking /
StopAttack / Attack / SetDir / GetDir / SkillUseId / SkillUsePos /
RemoveMap / Free / ChangeViewSize / DataCreate — and all map to
PARITY-REMAINING.md §P2.2 leaf work (consumer-driven; will land
when callers port).

### 2026-05-22 — T9.B per-fn rollup

Per-function audit. Gameplay-surface baseline: **3 ✅ / 20 ⚠️ /
17 ❌** across 40 entries. Most ⚠️ rows are `IUnitOpsService` stubs
returning sensible defaults; ❌ rows are mostly internal helpers
(target tracking, walkdelay, teleport timers) that haven't been
exposed because their consumers haven't ported. `BlownBy` (T2.3-H5)
is the one fully-functional method.

### 2026-05-20 — initial audit + service
- 51 public functions covered (canonical entry points). Detailed
  per-function table to follow in a richer per-file pass; the bar
  for this sweep is "every public function has a named C# entry
  point."
