using Map.Server.Entities;

namespace Map.Server.Gm.Groups;

/// <summary>
/// Player-group permission + command-allowed checks. Canonical entry
/// points for rAthena <c>pc_groups.cpp</c> (406 lines, 10 functions).
///
/// The C# port already ships a <c>groups.yml</c> loader
/// (<see cref="IPlayerGroupConfig"/>) and a <c>IPermissionService</c>
/// that resolves PC permissions; this service exposes the rAthena-named
/// entry points that scripts + atcommands call (`has_permission`,
/// `can_use_command`, `should_log_commands`).
/// </summary>
public interface IPlayerGroupsService
{
    /// <summary>rAthena <c>s_player_group::has_permission</c>.</summary>
    bool HasPermission(PlayerEntity pc, string permName);

    /// <summary>rAthena <c>s_player_group::can_use_command</c>.</summary>
    bool CanUseCommand(PlayerEntity pc, string commandName);

    /// <summary>rAthena <c>s_player_group::should_log_commands</c>.</summary>
    bool ShouldLogCommands(PlayerEntity pc);

    /// <summary>rAthena <c>pc_group_pc_load</c> — hydrate the group onto the PC at login.</summary>
    void PcLoad(PlayerEntity pc, int groupId);

    /// <summary>rAthena <c>pc_groups_reload</c>.</summary>
    void Reload();
}
