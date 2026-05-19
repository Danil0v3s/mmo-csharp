using Map.Server.Entities;

namespace Map.Server.Gm;

/// <summary>
/// One GM command. Discovered via DI registration in [Program.cs] and
/// indexed by <see cref="Name"/> (canonical, lowercase) in
/// <see cref="IGmCommandRegistry"/>.
/// <para>Permission gating lives in <c>conf/groups.yml</c>
/// (<see cref="Config.IPlayerGroupConfig"/>) — rAthena's model. The
/// command itself doesn't carry a minimum group id; the registry checks
/// the caller's group + permission set before dispatching.</para>
/// </summary>
public interface IGmCommand
{
    /// <summary>Canonical lowercase name, without the leading <c>@</c> or <c>#</c>. Aliases live in <c>atcommands.yml</c>.</summary>
    string Name { get; }

    /// <summary>One-line description (used by <c>@help</c> fallback when YAML help is empty).</summary>
    string Description { get; }

    /// <summary>
    /// Run the command. <paramref name="caller"/> is the invoking player;
    /// <paramref name="args"/> is the space-separated argument list (no
    /// command name, no leading symbol).
    /// </summary>
    Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct);
}
