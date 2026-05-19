using Map.Server.Gm.Config;
using Map.Server.Session;

namespace Map.Server.Gm;

/// <summary>
/// Concrete <see cref="IGmCommandRegistry"/>. Indexes by canonical name
/// and consults <see cref="IAtCommandConfig"/> on lookup so any alias
/// from <c>atcommands.yml</c> routes to the same handler.
///
/// Permission gating runs through <see cref="IPermissionService"/> +
/// <see cref="IPlayerGroupConfig"/> — rAthena <c>can_use_command</c>:
/// <list type="number">
///   <item><c>all_commands</c> permission → allow.</item>
///   <item>Group's <c>Commands:</c> list contains the canonical name → allow.</item>
///   <item><c>command_enable</c> permission + caller typed the at-prefix → allow
///         (rAthena's "unlock disabled commands for this session" toggle).</item>
///   <item>Otherwise refuse.</item>
/// </list>
/// </summary>
public sealed class GmCommandRegistry : IGmCommandRegistry
{
    private readonly Dictionary<string, IGmCommand> _byName;
    private readonly IAtCommandConfig _aliases;
    private readonly IPermissionService _perm;

    public GmCommandRegistry(
        IEnumerable<IGmCommand> commands,
        IAtCommandConfig aliases,
        IPermissionService perm)
    {
        _byName = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        _aliases = aliases;
        _perm = perm;
    }

    public IGmCommand? Get(string name)
    {
        if (_byName.TryGetValue(name, out var c)) return c;
        // Aliases route to canonical; try once more.
        var canonical = _aliases.ResolveAlias(name);
        if (!string.Equals(canonical, name, StringComparison.OrdinalIgnoreCase)
            && _byName.TryGetValue(canonical, out c))
        {
            return c;
        }
        return null;
    }

    public bool CanInvoke(MapSessionData session, IGmCommand cmd)
    {
        if (_perm.Has(session, PcPermission.UseAllCommands)) return true;
        if (_perm.CanUseAtCommand(session, cmd.Name)) return true;
        // command_enable matches rAthena's per-session unlock — once
        // toggled, the player can run any atcommand. We don't have a
        // session toggle yet, so treat the permission alone as the
        // unlock.
        if (_perm.Has(session, PcPermission.EnableCommand)) return true;
        return false;
    }

    public IEnumerable<IGmCommand> All() => _byName.Values;
}
