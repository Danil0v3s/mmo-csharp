namespace Map.Server.Gm.Config;

/// <summary>
/// One row from <c>conf/atcommands.yml</c>. Mirrors rAthena
/// <c>AtCommandDatabase</c> (atcommand.cpp). Aliases route to the same
/// command name at lookup; <see cref="Help"/> backs <c>@help &lt;cmd&gt;</c>.
/// </summary>
public sealed record AtCommandEntry(
    string Command,
    IReadOnlyList<string> Aliases,
    string Help);
