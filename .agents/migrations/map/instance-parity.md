# instance.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T8.5 per-fn table)

`src/map/instance.cpp` (1316 lines, 17 public functions).
Per-party / per-character map instances (dungeons, scripted single-PC content).

Canonical entry points: [IInstanceService](/Map.Server/Instance/IInstanceService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `instance_create` | ✅ | `IInstanceService.Create` |
| `instance_addmap` | ❌ | No C# entry; instance_create currently allocates a single map. Per-instance map-slot list (multiple maps inside one instance, e.g. Endless Tower) is the gap |
| `instance_addusers` | ✅ | `IInstanceService.AddUsers` |
| `instance_delusers` | ✅ | `IInstanceService.DelUsers` |
| `instance_destroy` | ✅ | `IInstanceService.Destroy` |
| `instance_destroy_command` | ✅ | `IInstanceService.DestroyCommand` |
| `instance_enter` | ❌ | No C# entry. Player → instance map handoff is the gap (movement service has no instance-aware warp). |
| `instance_reqinfo` | ✅ | `IInstanceService.ReqInfo` |
| `instance_startidletimer` | ✅ | `IInstanceService.StartIdleTimer` |
| `instance_stopidletimer` | ✅ | `IInstanceService.StopIdleTimer` |
| `instance_startkeeptimer` | ✅ | `IInstanceService.StartKeepTimer` |
| `instance_addnpc` | ✅ | `IInstanceService.AddNpc` |
| `instance_generate_mapname` | ✅ | `IInstanceService.GenerateMapName` |
| `instance_getsd` | ✅ | `IInstanceService.GetOwner` |
| `instance_mapid` | ❌ | No C# helper. Resolve "base map id + instance id → instance-specific map id." Needed for per-instance NPC placement / cell lookups. |
| `do_init_instance` | ⚠️ | rAthena lifecycle hook; C# DI registration covers init implicitly. No explicit method on `IInstanceService`. |
| `do_final_instance` | ⚠️ | Same — covered by DI dispose. No explicit method. |
| `do_reload_instance` | ✅ | `IInstanceService.Reload` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| **Totals** | **12** | **2** | **3** | **17** |

## Gaps in priority order

1. **`instance_enter`** (player UX critical) — without it, players can't actually walk into a freshly-created instance. Currently instance creation succeeds but the warp-to-instance path is missing.
2. **`instance_addmap` + `instance_mapid`** — multi-map instances (Endless Tower, Memorial Dungeon) won't work; single-map instances do.
3. **`do_init_instance` / `do_final_instance`** — cosmetic; DI covers the lifecycle.

## History

### 2026-05-22 — T8.5 per-function table

Replaced the prose "17 functions covered" claim with a per-function
audit table. Identified **3 ❌ + 2 ⚠️** that the prior pass missed:
- `instance_addmap` / `instance_mapid` / `instance_enter` are genuinely
  not in `IInstanceService`.
- `do_init_instance` / `do_final_instance` are covered by DI implicitly
  but lack an explicit method.

instance_db YAML data-pending stays as a separate dependency (parent
content track).

### 2026-05-20 — initial audit + service
- 17 public functions covered (canonical entry points; data-pending
  on parent dependency).
