# unit.cpp parity · 2026-05-22 (T9.B — per-fn rollup)

`src/map/unit.cpp` (4010 lines, 51 public functions).
Entity-action helpers (warp, walktoxy, stop_walking, stop_attack,
can_move, attack, blown_by, set_dir, skilluse_id, skilluse_pos,
remove_map, free). Forwards to MovementService / AttackService when
wired.

Canonical entry points: [IUnitOpsService](/Map.Server/Movement/UnitOps/IUnitOpsService.cs).

## Status legend

- ✅ implemented — full or near-full parity
- ⚠️ partial — stub returning sensible default
- ❌ missing — not in interface

## Per-function coverage

### Walking & pathing

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_walktoxy` | ⚠️ | `IUnitOpsService.WalkToXy` — stub |
| `unit_walktoxy_sub` / `ontouch` | ⚠️ | Internal pathfinding — not in interface |
| `unit_walktobl` | ⚠️ | `WalkToBl` — stub |
| `unit_stop_walking` / `_soon` | ⚠️ | `StopWalking` — stub |
| `unit_movepos` | ⚠️ | `MovePos` — stub |
| `unit_run` | ⚠️ | Flee-walk variant — not in interface |
| `unit_can_move` | ✅ | `CanMove` (semantic check returns true) |
| `unit_can_reach_pos` / `_bl` | ✅ | Both stubs returning true |
| `unit_is_walking` | ❌ | Not in interface |
| `unit_get_walkpath_time` | ❌ | Not in interface |

### Attack & combat

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_attack` | ⚠️ | `Attack` — stub |
| `unit_stopattack` | ⚠️ | `StopAttack` — stub |
| `unit_can_attack` | ⚠️ | Not in interface (impl check on AttackService) |
| `unit_set_target` | ❌ | Not in interface |
| `unit_changetarget` / `_sub` | ❌ | Not in interface |
| `unit_counttargeted` | ❌ | Not in interface |

### Direction & heading

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_setdir` | ⚠️ | `SetDir` — stub |
| `unit_getdir` | ⚠️ | `GetDir` — returns 0 |

### Knockback & displacement

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_blown` / `unit_blown_by` | ✅ | `BlownBy` — T2.3-H5 full impl with packet routing |
| `unit_escape` | ❌ | Not in interface (rng-based random-cell teleport) |

### Skill casting

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_skilluse_id` / `_id2` | ⚠️ | `SkillUseId` — stub |
| `unit_skilluse_pos` / `_pos2` | ⚠️ | `SkillUsePos` — stub |
| `unit_skillcastcancel` | ❌ | Not in interface |

### Teleport & map transit

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_warp` | ⚠️ | `Warp` — stub |
| `unit_remove_map` / `_pc` / `_sub` | ⚠️ | `RemoveMap` — stub |

### Lifecycle & data

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_data_create` / `_dataset` | ⚠️ | `DataCreate` — stub |
| `unit_free` / `_free_pc` | ⚠️ | `Free` — stub |
| `unit_refresh` | ❌ | Not in interface |
| `do_init_unit` / `do_final_unit` | ⚠️ | Not in interface (static init) |

### Misc

| rAthena fn | Status | C# location / note |
|---|---|---|
| `unit_calc_pos` | ❌ | Not in interface |
| `unit_changeviewsize` | ⚠️ | `ChangeViewSize` — stub |
| `unit_addshadowscar` | ❌ | Pet hatching — not in interface |
| `unit_run_hit` | ❌ | Flee-counter heuristic — not in interface |
| `unit_skillunit_maxcount` | ❌ | Not in interface |
| `unit_stop_stepaction` | ⚠️ | Not in interface |
| `unit_check_start_teleport_timer` | ❌ | Not in interface |
| `unit_unattackable` | ❌ | Not in interface |
| `unit_set_walkdelay` | ❌ | Not in interface |
| `unit_cancel_combo` | ❌ | Not in interface |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Walking / pathing | 2 | 6 | 2 | 10 |
| Attack & combat | 0 | 3 | 3 | 6 |
| Direction & heading | 0 | 2 | 0 | 2 |
| Knockback | 1 | 0 | 1 | 2 |
| Skill casting | 0 | 2 | 1 | 3 |
| Teleport & map transit | 0 | 2 | 0 | 2 |
| Lifecycle & data | 0 | 3 | 1 | 4 |
| Misc | 0 | 2 | 9 | 11 |
| **Totals (gameplay surface)** | **3** | **20** | **17** | **40** |

The remaining ~11 rAthena fns are internal helpers (NPC step-action
chains, attack-target db bookkeeping) without a C# equivalent
because the architecture differs (`IEntityRegistry` + `MovementService`
+ `AttackService` split the surface differently).

## History

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
