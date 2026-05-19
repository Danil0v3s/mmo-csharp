using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Map.Server.Gm.Config;

/// <summary>
/// YAML-backed <see cref="IAtCommandConfig"/>. Parses rAthena's
/// <c>conf/atcommands.yml</c> structure:
/// <code>
/// Body:
///   - Command: heal
///     Aliases:
///       - hp
///     Help: |
///       Heals the target ...
/// </code>
/// Aliases form a flat alias→canonical map so a single <see cref="Get"/>
/// can serve both forms. We deliberately don't validate Help (rAthena
/// treats it as opaque display text).
/// </summary>
public sealed class AtCommandConfig : IAtCommandConfig
{
    private readonly Dictionary<string, AtCommandEntry> _byName;
    private readonly Dictionary<string, string> _aliasToCanonical;

    public int Count => _byName.Count;

    public AtCommandConfig(string yamlPath, ILogger<AtCommandConfig> logger)
    {
        _byName = new(StringComparer.OrdinalIgnoreCase);
        _aliasToCanonical = new(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(yamlPath))
        {
            logger.LogWarning("atcommands.yml not found at {Path} — running with empty registry", yamlPath);
            return;
        }

        using var reader = File.OpenText(yamlPath);
        var stream = new YamlStream();
        stream.Load(reader);
        if (stream.Documents.Count == 0) return;
        if (stream.Documents[0].RootNode is not YamlMappingNode root) return;
        if (!root.Children.TryGetValue("Body", out var bodyNode) || bodyNode is not YamlSequenceNode body)
        {
            logger.LogWarning("atcommands.yml has no Body sequence");
            return;
        }

        foreach (var item in body.Children.OfType<YamlMappingNode>())
        {
            var name = ReadScalar(item, "Command");
            if (string.IsNullOrEmpty(name)) continue;

            var aliases = new List<string>();
            if (item.Children.TryGetValue("Aliases", out var aliasNode)
                && aliasNode is YamlSequenceNode aliasSeq)
            {
                foreach (var a in aliasSeq.Children.OfType<YamlScalarNode>())
                {
                    if (!string.IsNullOrWhiteSpace(a.Value)) aliases.Add(a.Value!);
                }
            }
            var help = ReadScalar(item, "Help") ?? string.Empty;

            var entry = new AtCommandEntry(name, aliases, help);
            _byName[name] = entry;
            foreach (var alias in aliases)
            {
                _aliasToCanonical[alias] = name;
            }
        }

        logger.LogInformation(
            "atcommands.yml loaded — {Cmds} commands, {Aliases} aliases",
            _byName.Count, _aliasToCanonical.Count);
    }

    public AtCommandEntry? Get(string nameOrAlias)
    {
        if (_byName.TryGetValue(nameOrAlias, out var entry)) return entry;
        if (_aliasToCanonical.TryGetValue(nameOrAlias, out var canonical)
            && _byName.TryGetValue(canonical, out entry)) return entry;
        return null;
    }

    public string ResolveAlias(string nameOrAlias)
        => _aliasToCanonical.TryGetValue(nameOrAlias, out var canonical) ? canonical : nameOrAlias;

    public IEnumerable<AtCommandEntry> All() => _byName.Values;

    private static string? ReadScalar(YamlMappingNode node, string key)
        => node.Children.TryGetValue(key, out var v) && v is YamlScalarNode s ? s.Value : null;
}
