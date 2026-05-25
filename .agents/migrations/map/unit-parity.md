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
| `unit_walktoxy` | ⚠️ | `IUnitOpsService.WalkToXy` still returns false; **MovementService.TryStartWalk is the real entry point everywhere else** (callers go direct). UnitOps shell needs to delegate. |
| `unit_walktoxy_sub` / `_ontouch` / `_nextcell` | ⚠️ | Internal step-tick lives in `MovementService.ScheduleNextStep` + `WalkState`; ontouch warp dispatch lives in `MovementService.OnArrive` → `IWarpDispatcher.OnEnterWarp`. No public C# surface (intentional — wraps inside MovementService). |
| `unit_walktobl` | ⚠️ | `IUnitOpsService.WalkToBl` stubbed; the AI engine uses `MovementService.TryStartWalk(target.X, target.Y)` directly. Shell needs delegate. |
| `unit_stop_walking` / `_soon` | ⚠️ | `IUnitOpsService.StopWalking` stubbed; `MovementService.CancelWalk` is the real call. Shell needs delegate. |
| `unit_movepos` | ✅ | `UnitOpsService.MovePos` (teleport-step; walkable gate + clif_slide/fixpos). |
| `unit_run` / `_run_hit` | ❌ | Flee-walk skill (Wind Walk Forced) variant — not in interface. |
| `unit_can_move` | ⚠️ | `IUnitOpsService.CanMove` always returns true; should read SC OPT1 + `Entity.WalkableAfterTick` + cast state. |
| `unit_can_reach_pos` / `_bl` | ⚠️ | Both return true; should use `IPathService.HasPath`. |
| `unit_is_walking` | ⚠️ | Not in interface but trivially `entity.Walk != null`. |
| `unit_get_walkpath_time` | ❌ | Not in interface; rAthena helper for predicting arrival tick. |
| `unit_calc_pos` | ❌ | Compute "follow position" for slave AI — not in interface. |
| `unit_update_chase` | ❌ | Re-evaluate AI chase target distance — not in interface. |

### Attack & combat

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_attack` | ⚠️ | `IUnitOpsService.Attack` stubbed; `IAttackService.StartAttack` is the real surface (Wave 71 wires `Targeters`). Shell needs delegate. |
| `unit_stop_attack` / `unit_stopattack` | ⚠️ | `IUnitOpsService.StopAttack` stubbed; `IAttackService.StopAttack` is the real call. Shell needs delegate. |
| `unit_can_attack` | ⚠️ | Not in interface; `AttackService.StartAttack` returns false on the same validation set. Public canonical helper still missing. |
| `unit_set_target` | ❌ | Not in interface; AttackService.StartAttack covers the latch but the standalone "swap target without restarting" helper isn't exposed. |
| `unit_changetarget` / `_sub` | ❌ | Not in interface; same as `set_target` but explicit. |
| `unit_unattackable` | ❌ | Not in interface; releases the attack lock + adds a brief immunity window. |
| `unit_counttargeted` | ✅ | Wave 71 — `Entity.Targeters` int counter, maintained by `AttackService.StartAttack` / `StopAttack`. Read directly from any consumer. |
| `unit_set_attackdelay` | ✅ | Wave 69 — `IAttackService.SetAttackDelay` (canonical entry; pushes `AttackState.AttackableTick`). |

### Direction & heading

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_setdir` | ⚠️ | `IUnitOpsService.SetDir` stubbed; `Entity.Dir` exists (read-only public). Shell needs to write + broadcast `ZC_CHANGE_DIRECTION`. |
| `unit_getdir` | ⚠️ | `IUnitOpsService.GetDir` returns 0; trivial fix — read `Entity.Dir`. |

### Knockback & displacement

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_blown` / `unit_blown_by` | ✅ | `UnitOpsService.BlownBy` (T2.3-H5; `IPathService.BlownPos` + clif_slide + clif_fixpos AOI). |
| `unit_escape` | ❌ | Not in interface (rng-based random-cell teleport, mob-AI flee). |

### Skill casting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_skilluse_id` / `_id2` | ⚠️ | `IUnitOpsService.SkillUseId` stubbed; `ISkillCastService.StartCast` is the real surface. Shell needs delegate. |
| `unit_skilluse_pos` / `_pos2` | ⚠️ | `IUnitOpsService.SkillUsePos` stubbed; `ISkillCastService.StartCastAt` is the real surface. Shell needs delegate. |
| `unit_skillcastcancel` | ❌ | Not in interface; `ISkillCastService.CancelCast` exists but no `IUnitOpsService` façade row. |
| `unit_cancel_combo` | ❌ | Not in interface; status-engine integration for SC_COMBO state. |

### Teleport & map transit

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_warp` | ⚠️ | `IUnitOpsService.Warp` returns 0; should call into `IWarpDispatcher.OnEnterWarp` (for PCs) / direct `EntityRegistry.Move` (for mobs). |
| `unit_remove_map` / `_pc` / `_sub` | ⚠️ | `IUnitOpsService.RemoveMap` stubbed; should call `IVisibilityService.NotifyVanishedToArea(reason)` + `IEntityRegistry.Remove`. |
| `unit_check_start_teleport_timer` | ❌ | Not in interface; tracks rewarp-loop count (anti-loop guard on warp portals). |
| `unit_get_masterteleport_timer` | ❌ | Not in interface; pet/homun "master teleported away" countdown. |
| `unit_set_walkdelay` | ✅ | Wave 69 — `IMovementService.SetWalkDelay` (canonical entry; stamps `Entity.WalkableAfterTick`). |

### Lifecycle & data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_dataset` / `unit_data_create` | ⚠️ | `IUnitOpsService.DataCreate` no-op; C# `Entity` allocates state inline via field initializers — there's no rAthena-style `unit_data` heap struct. Shell stays a no-op but the comment needs to call this out. |
| `unit_free` / `_free_pc` | ⚠️ | `IUnitOpsService.Free` returns 0; for PCs should drop walk/attack/cast state; for mobs should free spawn slot via `IMobSpawnService`. |
| `unit_refresh` | ❌ | Not in interface; refreshes the entity's wire state after gear / view changes. |
| `do_init_unit` / `do_final_unit` | ⚠️ | Static module init — C# DI replaces it. No-op by design. |
| `unit_changeviewsize` | ⚠️ | `IUnitOpsService.ChangeViewSize` returns 0; should write size + broadcast `ZC_NOTIFY_EFFECT2`. |
| `unit_addshadowscar` | ❌ | Pet-hatch shadow-scar visual; not in interface (pet system port pending). |
| `unit_skillunit_maxcount` | ❌ | Per-skill cap on simultaneous ground units; not in interface — `ISkillUnitService` enforces inline. |
| `unit_stop_stepaction` | ⚠️ | Walk-then-trigger chain (skill-on-arrive). `MovementService.OnArrive` already runs `IWarpDispatcher`; the broader step-action queue isn't exposed. |
| `unit_set_castdelay` | ❌ | Not in interface; `ISkillCastService` reads cast delay from skill_db inline. |
| `unit_changetarget_sub` | ❌ | Internal helper for the registry iter inside `unit_changetarget`. Not a public surface. |

### Misc

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_changetarget` | ❌ | See Attack section. |
| `unit_data::getpos` | ✅ | `(Entity.X, Entity.Y, Entity.MapId)` — direct field reads. |
| `unit_data::update_pos` | ✅ | `IEntityRegistry.Move`. |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Walking / pathing | 1 | 7 | 4 | 12 |
| Attack & combat | 2 | 3 | 4 | 9 |
| Direction & heading | 0 | 2 | 0 | 2 |
| Knockback | 1 | 0 | 1 | 2 |
| Skill casting | 0 | 2 | 2 | 4 |
| Teleport & map transit | 1 | 2 | 2 | 5 |
| Lifecycle & data | 0 | 4 | 5 | 9 |
| Misc | 2 | 0 | 1 | 3 |
| **Totals (gameplay surface)** | **7** | **20** | **19** | **46** |

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
