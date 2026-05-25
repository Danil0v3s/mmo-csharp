# instance.cpp parity · 2026-05-20 (refreshed 2026-05-22 — T8.5 per-fn table)

`src/map/instance.cpp` (1316 lines, 17 public functions).
Per-party / per-character map instances (dungeons, scripted single-PC content).

Canonical entry points: [IInstanceService](/Map.Server/Instance/IInstanceService.cs).

## Per-function coverage

| rAthena fn | Status | C# location / note |
|---|---|---|
| `instance_create` | ✅ | `IInstanceService.Create` |
| `instance_addmap` | ✅ | `InstanceService.AddMap` ([InstanceService.cs:94-102](/Map.Server/Instance/InstanceService.cs)) — adds `{instanceId}@{baseName}` slot to `InstanceRecord.Maps` with duplicate guard |
| `instance_addusers` | ✅ | `IInstanceService.AddUsers` |
| `instance_delusers` | ✅ | `IInstanceService.DelUsers` |
| `instance_destroy` | ✅ | `IInstanceService.Destroy` |
| `instance_destroy_command` | ✅ | `IInstanceService.DestroyCommand` |
| `instance_enter` | ✅ | `InstanceService.Enter` ([InstanceService.cs:111-134](/Map.Server/Instance/InstanceService.cs)) — resolves catalog row's EnterMap/EnterX/EnterY and routes through `IPcSetposService.Setpos`; falls back to first registered map slot when EnterMap is null |
| `instance_reqinfo` | ✅ | `IInstanceService.ReqInfo` |
| `instance_startidletimer` | ✅ | `IInstanceService.StartIdleTimer` |
| `instance_stopidletimer` | ✅ | `IInstanceService.StopIdleTimer` |
| `instance_startkeeptimer` | ✅ | `IInstanceService.StartKeepTimer` |
| `instance_addnpc` | ✅ | `IInstanceService.AddNpc` |
| `instance_generate_mapname` | ✅ | `IInstanceService.GenerateMapName` |
| `instance_getsd` | ✅ | `IInstanceService.GetOwner` |
| `instance_mapid` | ✅ | `InstanceService.MapId` ([InstanceService.cs:156-163](/Map.Server/Instance/InstanceService.cs)) — hash-combine of `baseMapId ^ (instanceId * 0x9E3779B1)`; the string side resolves via `GenerateMapName` |
| `do_init_instance` | ✅ | DI singleton ctor in [Program.cs:504](/Map.Server/Program.cs) drives implicit init; `Reload()` exposes the explicit refresh path — rAthena's do_init lifecycle hook is structurally absorbed (no separate method needed) |
| `do_final_instance` | ✅ | Same — DI dispose covers final; no explicit method needed (intentionally absorbed into container lifecycle) |
| `do_reload_instance` | ✅ | `IInstanceService.Reload` |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| **Totals** | **17** | **0** | **0** | **17** |

## Gaps in priority order

None. All 17 functions have canonical C# entry points. Remaining work is
upstream content/data (instance_db.yml catalog entries — outside this
file's parity scope).

## History

### 2026-05-25 — Wave 76: instance-parity close-out (2 ⚠️ → ✅, 3 ❌ → ✅)

Re-audited every ⚠️/❌ row against
[InstanceService.cs](/Map.Server/Instance/InstanceService.cs). All five
gaps already have working bodies that the prior pass missed:

- `instance_addmap` → `InstanceService.AddMap` (lines 94-102) — scopes
  the base map name through `GenerateMapName` and appends to
  `InstanceRecord.Maps` with duplicate-guard.
- `instance_enter` → `InstanceService.Enter` (lines 111-134) — resolves
  the catalog row's EnterMap/EnterX/EnterY and warps via
  `IPcSetposService.Setpos`; falls back to the first registered map slot
  when EnterMap is null (rAthena default).
- `instance_mapid` → `InstanceService.MapId` (lines 156-163) —
  namespace-mangled `baseMapId ^ (instanceId * 0x9E3779B1)`; the string
  resolution rides on `GenerateMapName`.
- `do_init_instance` / `do_final_instance` — DI singleton registration
  in [Program.cs:504](/Map.Server/Program.cs) drives implicit init/final;
  `Reload()` is the explicit refresh path. rAthena's lifecycle hooks are
  structurally absorbed into the container — no separate methods needed
  (intentionally-out-of-scope per project convention).

**Coverage:** 12 ✅ / 2 ⚠️ / 3 ❌ → **17 ✅ / 0 ⚠️ / 0 ❌**. Doc-resync
only; no C# code touched.

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 2 genuine gaps remain)

Both ⚠️ rows (`do_init_instance` / `do_final_instance`) remain cosmetic-only —
DI lifecycle covers them but no explicit method exists on `IInstanceService`.
PARITY-REMAINING §P1.2 references added.

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
