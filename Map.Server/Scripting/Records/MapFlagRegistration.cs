namespace Map.Server.Scripting.Records;

/// <summary>
/// One <c>registerMapFlag({ ... })</c> call. Mirrors rAthena's
/// <c>npc/re/mapflag/*.txt</c> declarative lines:
///   <c>&lt;mapname&gt;\tmapflag\t&lt;flag&gt;[\t&lt;value&gt;]</c>.
///
/// The flag string is preserved verbatim from the source; the actual
/// enforcement at runtime is a separate concern. A consumer hasn't been
/// wired yet — these registrations populate <c>INpcRegistry.AllMapFlags()</c>
/// so the surface is captured for when MapFlagService lands.
/// </summary>
public sealed record MapFlagRegistration
{
    public required string Map { get; init; }
    public required string Flag { get; init; }
    /// <summary>Optional value (e.g. <c>"1"</c>, <c>"100"</c>, "<c>2,1,5</c>"). Null when the flag is a bare switch.</summary>
    public string? Value { get; init; }
}
