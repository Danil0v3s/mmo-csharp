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
/// After AT-R + AT-C waves (2026-05-23) the only remaining stubs are
/// battleground queue commands (depend on the queue state machine
/// landing — tracked as a separate BG wave) + a handful of niche
/// observer commands. Everything else has a real <c>IGmCommand</c>
/// in this folder.
/// </summary>
internal static class StubCommandKinds
{
    /// <summary>One stub spec: command name + subsystem-pending label.</summary>
    public sealed record Spec(string Name, string Subsystem);

    /// <summary>
    /// Curated list of atcommands whose backend isn't ported yet.
    /// </summary>
    public static readonly Spec[] Specs =
    {
        // battleground.cpp queue commands — depend on the QUEUE_STATE_*
        // state machine which lands with the dedicated BG wave.
        new("bgsmall", "battleground.cpp"), new("bgmedium", "battleground.cpp"),
        new("bglarge", "battleground.cpp"), new("bg", "battleground.cpp"),
        new("bgstart", "battleground.cpp"), new("bgend", "battleground.cpp"),
        new("bgleave", "battleground.cpp"), new("bgleader", "battleground.cpp"),
        new("bginvite", "battleground.cpp"),
        // option bitmask + display status — pending OPT2/OPT3 toggle table
        new("option", "pc.cpp:option"),
        new("displaystatus", "pc.cpp:status"),
        // who-variants (display formatting only — defer the per-variant
        // formatter shape until the wire layer rework)
        new("who2", "pc.cpp:who"), new("who3", "pc.cpp:who"),
        new("whomap", "pc.cpp:who"), new("whomap2", "pc.cpp:who"),
        new("whomap3", "pc.cpp:who"), new("whogm", "pc.cpp:who"),
        // mapexit2 — defer alongside @mapexit (server-shutdown coordinator
        // is its own thing)
        new("mapexit2", "map.cpp"),
        // baselevelup — alias of @level (existing LevelCommand). Stub
        // kept to maintain the canonical name → no-op route until the
        // alias table absorbs it.
        new("baselevelup", "pc.cpp:joblevel"),
        // spiritball / soulball — real impls already registered
        // (SpiritballCommand / SoulballCommand). No stub needed.
    };
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
