using YamlDotNet.RepresentationModel;

namespace Tools.RathenaImporter;

/// <summary>
/// Light traversal helpers over <see cref="YamlMappingNode"/>. rAthena
/// YAML headers are all
///
/// <code>
/// Header:
///   Type: SKILL_DB
///   Version: 4
/// Body:
///   - Id: ...
///     Name: ...
///     ...
/// </code>
///
/// — so the same shape works everywhere. These helpers unwrap the
/// Body array, pull typed fields out of a row, and resolve the
/// per-level <c>[{Level: N, Amount: X}]</c> arrays rAthena uses.
/// </summary>
internal static class YamlHelpers
{
    /// <summary>Open a rAthena YAML file, return the <c>Body</c> sequence.</summary>
    public static YamlSequenceNode LoadBody(string path)
    {
        using var reader = File.OpenText(path);
        var stream = new YamlStream();
        stream.Load(reader);
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        return (YamlSequenceNode)root.Children[new YamlScalarNode("Body")];
    }

    /// <summary>True if the map contains <paramref name="key"/>.</summary>
    public static bool Has(this YamlMappingNode node, string key)
        => node.Children.ContainsKey(new YamlScalarNode(key));

    /// <summary>Get a child node or null.</summary>
    public static YamlNode? Get(this YamlMappingNode node, string key)
    {
        var k = new YamlScalarNode(key);
        return node.Children.TryGetValue(k, out var v) ? v : null;
    }

    /// <summary>Get a scalar string, or null if absent / non-scalar.</summary>
    public static string? Str(this YamlMappingNode node, string key)
        => (node.Get(key) as YamlScalarNode)?.Value;

    /// <summary>Get a scalar int, or null.</summary>
    public static int? Int(this YamlMappingNode node, string key)
    {
        var v = node.Str(key);
        if (v == null) return null;
        return int.TryParse(v, out var n) ? n : null;
    }

    /// <summary>Get a scalar long, or null.</summary>
    public static long? Long(this YamlMappingNode node, string key)
    {
        var v = node.Str(key);
        if (v == null) return null;
        return long.TryParse(v, out var n) ? n : null;
    }

    /// <summary>
    /// rAthena scalar bool — accepts "true" / "false" (case-insensitive).
    /// Missing → null; non-bool scalar → null.
    /// </summary>
    public static bool? Bool(this YamlMappingNode node, string key)
    {
        var v = node.Str(key);
        if (v == null) return null;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) ? true
            : v.Equals("false", StringComparison.OrdinalIgnoreCase) ? false
            : (bool?)null;
    }

    /// <summary>Iterate child rows of a sequence-valued field.</summary>
    public static IEnumerable<YamlMappingNode> Rows(this YamlMappingNode node, string key)
    {
        if (node.Get(key) is not YamlSequenceNode seq) yield break;
        foreach (var c in seq.Children)
            if (c is YamlMappingNode m) yield return m;
    }

    /// <summary>Iterate a sequence directly.</summary>
    public static IEnumerable<YamlMappingNode> Rows(this YamlSequenceNode seq)
    {
        foreach (var c in seq.Children)
            if (c is YamlMappingNode m) yield return m;
    }

    /// <summary>
    /// Flatten a rAthena per-level array (e.g. <c>SpCost: [{Level: 1, Amount: 8}, …]</c>)
    /// into a `:`-delimited string indexed by level (1-based). Missing
    /// levels fall through to the previous value (rAthena semantics).
    /// </summary>
    public static string PackPerLevel(
        this YamlMappingNode node, string field, string valueKey,
        int maxLevel, string fallback = "0")
    {
        var seq = node.Get(field) as YamlSequenceNode;
        var values = new string[maxLevel + 1];
        values[0] = "0"; // index 0 unused
        var lastValue = fallback;
        if (seq != null)
        {
            // Build a per-level lookup.
            var byLevel = new Dictionary<int, string>();
            foreach (var row in seq.Rows())
            {
                var lvl = row.Int("Level");
                var amt = row.Str(valueKey);
                if (lvl is null || amt is null) continue;
                byLevel[lvl.Value] = amt;
            }
            for (var i = 1; i <= maxLevel; i++)
            {
                if (byLevel.TryGetValue(i, out var v)) lastValue = v;
                values[i] = lastValue;
            }
        }
        else
        {
            for (var i = 1; i <= maxLevel; i++) values[i] = fallback;
        }
        // emit "v1:v2:v3:..."
        return string.Join(':', values.Skip(1));
    }
}
