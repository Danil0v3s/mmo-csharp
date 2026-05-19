namespace Map.Server.Gm.Config;

/// <summary>
/// One <c>conf/groups.yml</c> entry after inheritance resolution.
/// Mirrors rAthena <c>struct s_player_group</c> (pc_groups.hpp:84) — id,
/// name, level, allowed atcommand / charcommand names, permission set,
/// LogCommands flag.
///
/// All collections here are post-inheritance: child groups already have
/// their parents' commands / permissions folded in, mirroring rAthena
/// <c>player_group_db.loadingFinished</c>.
/// </summary>
public sealed class PlayerGroup
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public int Level { get; init; }
    public bool LogCommands { get; init; }

    /// <summary>Atcommand names (canonical, lowercased) the group can invoke.</summary>
    public required HashSet<string> Commands { get; init; }
    /// <summary>Charcommand names (#cmd) the group can invoke.</summary>
    public required HashSet<string> CharCommands { get; init; }

    /// <summary>Resolved permission set.</summary>
    public required HashSet<PcPermission> Permissions { get; init; }
}
