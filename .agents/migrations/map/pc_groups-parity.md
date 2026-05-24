# pc_groups.cpp parity · 2026-05-22 (T9.F — per-fn rollup)

`src/map/pc_groups.cpp` (406 lines, 10 functions) — `groups.yml`
loader + per-PC permission resolution.

The C# port already ships `IPlayerGroupConfig` (yml loader) and
`IPermissionService` (session-keyed resolver). This audit just adds
the rAthena-named entry points so scripts that take a PlayerEntity
arg have a single call.

## Subsystem coverage

| rAthena fn | Status | C# location |
|---|---|---|
| `PlayerGroupDatabase::parseBodyNode` | ✅ | `IPlayerGroupConfig` (yml loader) |
| `PlayerGroupDatabase::parseCommands` | ✅ | same loader |
| `PlayerGroupDatabase::loadingFinished` | ✅ | same loader |
| `s_player_group::has_permission` | ✅ | [PlayerGroupsService.HasPermission](/Map.Server/Gm/Groups/PlayerGroupsService.cs) |
| `s_player_group::can_use_command` | ✅ | `PlayerGroupsService.CanUseCommand` |
| `s_player_group::should_log_commands` | ⚠️ | `PlayerGroupsService.ShouldLogCommands` returns default `true`; per-group YAML flag accessor in `IPlayerGroupConfig` pending. PARITY-REMAINING §P1.2 |
| `pc_group_pc_load` | ✅ | `PlayerGroupsService.PcLoad` |
| `pc_groups_reload` | ✅ | `PlayerGroupsService.Reload` |
| `do_init_pc_groups` / `do_final_pc_groups` | ✅ | DI lifecycle |

## Coverage summary

| Bucket | ✅ | ⚠️ | ❌ | Total |
|---|---|---|---|---|
| Group YAML loader + permission resolver | 9 | 1 | 0 | 10 |
| **Totals** | **9** | **1** | **0** | **10** |

## History

### 2026-05-24 — P2.1 doc-resync close-out (0 stale ⚠️ → ✅; 1 genuine gap remains)

Verified `ShouldLogCommands` still returns default-true with a TODO comment;
waiting for `IPlayerGroupConfig` to expose the per-group `log_commands` flag.
PARITY-REMAINING §P1.2 reference added.

### 2026-05-22 — T9.F per-fn rollup

Per-function audit. Baseline: **9 ✅ / 1 ⚠️ / 0 ❌**. Group YAML
loader (parseBodyNode / parseCommands / loadingFinished) + per-PC
permission resolver (HasPermission / CanUseCommand / PcLoad /
Reload) + DI lifecycle all ✅. Lone ⚠️ is `should_log_commands`
(knob default rather than real per-group flag — easy fix).

### 2026-05-20 — initial audit + service
- `IPlayerGroupsService` / `PlayerGroupsService` covers all 10 functions.
- `PlayerEntity.GroupId` added.
