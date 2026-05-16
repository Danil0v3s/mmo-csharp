namespace Map.Server.Mob;

/// <summary>
/// In-memory catalog of mob definitions. Loaded once at startup from
/// <c>mob_db.yml</c> (+ optional <c>mob_db2.yml</c> overrides). Read-only
/// at gameplay-tick time; reloads happen out-of-band via <see cref="Reload"/>.
/// </summary>
public interface IMobDb
{
    /// <summary>Number of distinct entries in the catalog.</summary>
    int Count { get; }

    /// <summary>Lookup by integer class id (the network/protocol id).</summary>
    MobDbEntry? Get(int classId);

    /// <summary>Lookup by aegis name (e.g. <c>"PORING"</c>). Case-insensitive.</summary>
    MobDbEntry? GetByAegisName(string aegisName);

    /// <summary>Enumerate every loaded mob.</summary>
    IEnumerable<MobDbEntry> All();

    /// <summary>Reparse the YAML files (GM <c>/reloadmobdb</c> equivalent).</summary>
    void Reload();
}
