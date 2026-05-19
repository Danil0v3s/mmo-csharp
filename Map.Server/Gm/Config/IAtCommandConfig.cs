namespace Map.Server.Gm.Config;

/// <summary>
/// In-memory view of <c>conf/atcommands.yml</c> (rAthena
/// <c>AtCommandDatabase</c>, atcommand.cpp:9920). 313 entries across the
/// 288 unique commands + 25 alias rows. Used by:
/// <list type="bullet">
///   <item><see cref="GmCommandRegistry"/> — alias resolution on lookup.</item>
///   <item><c>@help</c> — yields the per-command help string.</item>
///   <item><c>@commands</c> / <c>@charcommands</c> — iteration source.</item>
/// </list>
/// </summary>
public interface IAtCommandConfig
{
    /// <summary>Total entries (including alias rows).</summary>
    int Count { get; }

    /// <summary>Look up by canonical name OR alias. Returns null if unknown.</summary>
    AtCommandEntry? Get(string nameOrAlias);

    /// <summary>Resolve an alias to its canonical command name; returns the input if it's already canonical or unknown.</summary>
    string ResolveAlias(string nameOrAlias);

    /// <summary>Every canonical entry (excluding alias-only rows).</summary>
    IEnumerable<AtCommandEntry> All();
}
