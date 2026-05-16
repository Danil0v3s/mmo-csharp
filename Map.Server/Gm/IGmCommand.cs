using Map.Server.Entities;

namespace Map.Server.Gm;

/// <summary>
/// One GM command. Discovered via DI registration in [Program.cs] and
/// indexed by <see cref="Name"/> in <see cref="IGmCommandRegistry"/>.
/// Implementations should be cheap to instantiate (transient) — the
/// registry creates them per-invocation.
/// </summary>
public interface IGmCommand
{
    /// <summary>Lowercase name, without the leading <c>@</c> or <c>#</c>.</summary>
    string Name { get; }

    /// <summary>
    /// Minimum account <c>group_id</c> required to execute. rAthena's
    /// <c>conf/groups.conf</c> typically maps GroupId 1+ to test commands,
    /// 60+ to event-team, 99 to full admin. We mirror the numeric scheme;
    /// the canonical groups.conf import lands later.
    /// </summary>
    int MinGroupId { get; }

    /// <summary>One-line description (used by <c>@help</c> later).</summary>
    string Description { get; }

    /// <summary>
    /// Run the command. <paramref name="caller"/> is the invoking player;
    /// <paramref name="args"/> is the space-separated argument list (no
    /// command name, no leading symbol). Use
    /// <c>IVisibilityService.SendToSelf</c> / <c>SendToArea</c> for any
    /// player-visible output.
    /// </summary>
    Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct);
}
