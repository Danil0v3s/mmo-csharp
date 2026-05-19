namespace Map.Server.Gm.Config;

/// <summary>
/// In-memory view of <c>conf/groups.yml</c> (rAthena
/// <c>player_group_db</c>). Pre-resolved for inheritance — looking up a
/// group already includes everything its <c>Inherit:</c> ancestors
/// granted.
/// </summary>
public interface IPlayerGroupConfig
{
    PlayerGroup? Get(int id);

    /// <summary>
    /// rAthena <c>s_player_group::can_use_command</c> (pc_groups.cpp:325).
    /// <para><c>all_commands</c> permission grants every atcommand
    /// regardless of the <c>Commands:</c> list. The C# port mirrors that
    /// (parity over rationalization).</para>
    /// </summary>
    bool CanUseAtCommand(int groupId, string commandName);

    bool CanUseCharCommand(int groupId, string commandName);

    /// <summary>Permission check, post-inheritance.</summary>
    bool HasPermission(int groupId, PcPermission perm);

    IEnumerable<PlayerGroup> All();
}
