namespace Tools.RathenaImporter;

/// <summary>
/// rAthena mapflag line:
/// <code>mapname &lt;TAB&gt; mapflag &lt;TAB&gt; flag [&lt;TAB&gt; value]</code>
/// </summary>
public sealed record MapFlagRow(string MapName, string Flag, string Value);

public static class MapFlagParser
{
    public static IEnumerable<MapFlagRow> ParseFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var row = TryParse(raw);
            if (row != null) yield return row;
        }
    }

    public static MapFlagRow? TryParse(string raw)
    {
        var line = raw.TrimStart();
        if (line.Length == 0 || line.StartsWith("//")) return null;

        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;
        var directive = parts[1].Trim().ToLowerInvariant();
        if (directive != "mapflag") return null;

        var value = parts.Length >= 4 ? parts[3].Trim() : string.Empty;
        // Strip trailing inline comments.
        var commentIdx = value.IndexOf("//", StringComparison.Ordinal);
        if (commentIdx >= 0) value = value[..commentIdx].Trim();

        return new MapFlagRow(
            MapName: parts[0].Trim(),
            Flag: parts[2].Trim(),
            Value: value);
    }
}
