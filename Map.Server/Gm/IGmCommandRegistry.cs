using Map.Server.Session;

namespace Map.Server.Gm;

/// <summary>
/// Lookup table from command name → <see cref="IGmCommand"/>. Aliases
/// from <c>atcommands.yml</c> are resolved at <see cref="Get"/> time.
/// </summary>
public interface IGmCommandRegistry
{
    /// <summary>Resolves aliases. Returns null if no command (canonical or alias) matches.</summary>
    IGmCommand? Get(string name);

    /// <summary>
    /// True if <paramref name="session"/> may invoke <paramref name="cmd"/>.
    /// rAthena <c>pc_can_use_command</c>: group <c>Commands:</c> list OR
    /// <c>all_commands</c> permission; <c>command_enable</c> grants
    /// commands the group otherwise wouldn't have.
    /// </summary>
    bool CanInvoke(MapSessionData session, IGmCommand cmd);

    IEnumerable<IGmCommand> All();
}
