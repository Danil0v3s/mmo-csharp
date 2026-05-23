using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Gm.Commands;

/// <summary>
/// Catch-all for rAthena atcommands whose backend subsystem hasn't
/// ported yet. Each stub appears in the registry, passes group/permission
/// gating like a real command, and on invocation replies with a
/// well-formed "feature not yet ported — see X" message.
///
/// After AT-R + AT-C + AT-D1 waves (2026-05-23) every atcommand has a
/// real <c>IGmCommand</c> registered in this folder. The Specs list is
/// intentionally empty — the <see cref="StubCommand"/> class is kept as
/// the fallback shape so a future deferred command can be added without
/// reintroducing the boilerplate, but no entries are active.
/// </summary>
internal static class StubCommandKinds
{
    /// <summary>One stub spec: command name + subsystem-pending label.</summary>
    public sealed record Spec(string Name, string Subsystem);

    /// <summary>
    /// Curated list of atcommands whose backend isn't ported yet.
    /// Empty after AT-D1 — every stub was promoted to a real handler.
    /// </summary>
    public static readonly Spec[] Specs = Array.Empty<Spec>();
}

/// <summary>
/// One stub instance per <see cref="StubCommandKinds.Spec"/>. The
/// registration loop in <c>Program.cs</c> walks the spec list and adds
/// each.
/// </summary>
public sealed class StubCommand : IGmCommand
{
    private readonly string _subsystem;
    private readonly IVisibilityService _visibility;

    public string Name { get; }
    public string Description { get; }

    public StubCommand(string name, string subsystem, IVisibilityService visibility)
    {
        Name = name;
        _subsystem = subsystem;
        _visibility = visibility;
        Description = $"@{name} — pending: {subsystem} not yet ported";
    }

    public Task ExecuteAsync(PlayerEntity caller, IReadOnlyList<string> args, CancellationToken ct)
    {
        _visibility.SendToSelf(caller, new ZC_NOTIFY_PLAYERCHAT
        {
            Message = $"@{Name}: not yet implemented — {_subsystem} pending.",
        });
        return Task.CompletedTask;
    }
}
