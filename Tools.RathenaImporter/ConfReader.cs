namespace Tools.RathenaImporter;

/// <summary>
/// Reads rAthena's <c>scripts_*.conf</c> entry-point files. Each line
/// looks like <c>npc: npc/re/warps/cities/prontera.txt</c>. We honor
/// the leading-<c>npc:</c> directive and ignore commented-out
/// <c>//npc:</c> lines plus blank/comment-only lines.
/// </summary>
public static class ConfReader
{
    public static List<string> LoadReferencedFiles(string confPath, string rathenaRoot)
    {
        if (!File.Exists(confPath))
            throw new FileNotFoundException("conf file not found", confPath);

        var result = new List<string>();
        foreach (var raw in File.ReadAllLines(confPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            if (!line.StartsWith("npc:", StringComparison.OrdinalIgnoreCase)) continue;
            var rel = line["npc:".Length..].Trim();
            // strip inline comments
            var commentIdx = rel.IndexOf("//", StringComparison.Ordinal);
            if (commentIdx >= 0) rel = rel[..commentIdx].Trim();
            if (rel.Length == 0) continue;

            var abs = Path.Combine(rathenaRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(abs)) result.Add(abs);
            else Console.Error.WriteLine($"warn: referenced file not found, skipping: {rel}");
        }
        return result;
    }
}
