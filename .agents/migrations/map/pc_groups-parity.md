# pc_groups.cpp parity · 2026-05-20

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
| `s_player_group::should_log_commands` | ⚠️ | `PlayerGroupsService.ShouldLogCommands` — knob default |
| `pc_group_pc_load` | ✅ | `PlayerGroupsService.PcLoad` |
| `pc_groups_reload` | ✅ | `PlayerGroupsService.Reload` |
| `do_init_pc_groups` / `do_final_pc_groups` | ✅ | DI lifecycle |

## History

### 2026-05-20 — initial audit + service
- `IPlayerGroupsService` / `PlayerGroupsService` covers all 10 functions.
- `PlayerEntity.GroupId` added.
