using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Map.Server.Gm.Config;

/// <summary>
/// YAML-backed <see cref="IPlayerGroupConfig"/>. Two-pass load to mirror
/// rAthena's <c>PlayerGroupDatabase::loadingFinished</c>:
/// <list type="number">
///   <item>Parse each raw group entry (commands, char_commands, permissions, inherits).</item>
///   <item>Iterate <c>Inherit:</c> in dependency order, folding ancestor
///         commands / permissions into descendants.</item>
/// </list>
/// Cycles are detected and dropped with a warning; missing parent names
/// log a warning and skip the inherit edge.
/// </summary>
public sealed class PlayerGroupConfig : IPlayerGroupConfig
{
    private readonly Dictionary<int, PlayerGroup> _byId = new();
    private readonly ILogger<PlayerGroupConfig> _logger;

    public PlayerGroupConfig(string yamlPath, ILogger<PlayerGroupConfig> logger)
    {
        _logger = logger;
        if (!File.Exists(yamlPath))
        {
            logger.LogWarning("groups.yml not found at {Path} — no GM groups loaded", yamlPath);
            return;
        }

        using var reader = File.OpenText(yamlPath);
        var stream = new YamlStream();
        stream.Load(reader);
        if (stream.Documents.Count == 0) return;
        if (stream.Documents[0].RootNode is not YamlMappingNode root) return;
        if (!root.Children.TryGetValue("Body", out var bodyNode) || bodyNode is not YamlSequenceNode body)
        {
            logger.LogWarning("groups.yml has no Body sequence");
            return;
        }

        // Pass 1: raw entries keyed by name (for inherit lookup) and id.
        var raw = new Dictionary<string, RawGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in body.Children.OfType<YamlMappingNode>())
        {
            var idStr = ReadScalar(item, "Id");
            if (!int.TryParse(idStr, out var id)) continue;
            var name = ReadScalar(item, "Name") ?? $"Group{id}";
            var levelStr = ReadScalar(item, "Level");
            int.TryParse(levelStr, out var level);
            var logCmds = string.Equals(ReadScalar(item, "LogCommands"), "true", StringComparison.OrdinalIgnoreCase);

            var commands = ReadFlagMap(item, "Commands");
            var charCommands = ReadFlagMap(item, "CharCommands");
            var perms = ReadPermissions(item);
            var inherits = ReadFlagMap(item, "Inherit");

            raw[name] = new RawGroup(id, name, level, logCmds, commands, charCommands, perms, inherits);
        }

        // Pass 2: resolve inheritance. Walk in topological order — visit
        // ancestors before descendants. Cycle detection bails out with
        // partial state so a bad config doesn't crash the server.
        var resolved = new Dictionary<string, PlayerGroup>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, _) in raw)
        {
            Resolve(name, raw, resolved, visiting);
        }

        foreach (var g in resolved.Values)
        {
            _byId[g.Id] = g;
        }

        logger.LogInformation(
            "groups.yml loaded — {Count} groups, ids: [{Ids}]",
            _byId.Count, string.Join(",", _byId.Keys.OrderBy(i => i)));
    }

    private PlayerGroup Resolve(
        string name,
        Dictionary<string, RawGroup> raw,
        Dictionary<string, PlayerGroup> done,
        HashSet<string> visiting)
    {
        if (done.TryGetValue(name, out var cached)) return cached;
        if (!raw.TryGetValue(name, out var entry))
        {
            _logger.LogWarning("groups.yml — Inherit references unknown group {Name}", name);
            // Sentinel empty group so downstream lookups don't NRE.
            return new PlayerGroup
            {
                Id = -1, Name = name, Level = 0, LogCommands = false,
                Commands = new(StringComparer.OrdinalIgnoreCase),
                CharCommands = new(StringComparer.OrdinalIgnoreCase),
                Permissions = new(),
            };
        }
        if (!visiting.Add(name))
        {
            _logger.LogWarning("groups.yml — Inherit cycle detected at {Name} — skipping", name);
            return new PlayerGroup
            {
                Id = entry.Id, Name = entry.Name, Level = entry.Level, LogCommands = entry.LogCommands,
                Commands = new(entry.Commands, StringComparer.OrdinalIgnoreCase),
                CharCommands = new(entry.CharCommands, StringComparer.OrdinalIgnoreCase),
                Permissions = new(entry.Permissions),
            };
        }

        var commands = new HashSet<string>(entry.Commands, StringComparer.OrdinalIgnoreCase);
        var charCommands = new HashSet<string>(entry.CharCommands, StringComparer.OrdinalIgnoreCase);
        var perms = new HashSet<PcPermission>(entry.Permissions);

        foreach (var parentName in entry.Inherits)
        {
            var parent = Resolve(parentName, raw, done, visiting);
            commands.UnionWith(parent.Commands);
            charCommands.UnionWith(parent.CharCommands);
            perms.UnionWith(parent.Permissions);
        }

        var resolved = new PlayerGroup
        {
            Id = entry.Id,
            Name = entry.Name,
            Level = entry.Level,
            LogCommands = entry.LogCommands,
            Commands = commands,
            CharCommands = charCommands,
            Permissions = perms,
        };
        visiting.Remove(name);
        done[name] = resolved;
        return resolved;
    }

    public PlayerGroup? Get(int id) => _byId.GetValueOrDefault(id);

    public bool CanUseAtCommand(int groupId, string commandName)
    {
        var g = Get(groupId);
        if (g == null) return false;
        if (g.Permissions.Contains(PcPermission.UseAllCommands)) return true;
        return g.Commands.Contains(commandName);
    }

    public bool CanUseCharCommand(int groupId, string commandName)
    {
        var g = Get(groupId);
        if (g == null) return false;
        if (g.Permissions.Contains(PcPermission.UseAllCommands)) return true;
        return g.CharCommands.Contains(commandName);
    }

    public bool HasPermission(int groupId, PcPermission perm)
        => Get(groupId)?.Permissions.Contains(perm) ?? false;

    public IEnumerable<PlayerGroup> All() => _byId.Values;

    private static string? ReadScalar(YamlMappingNode node, string key)
        => node.Children.TryGetValue(key, out var v) && v is YamlScalarNode s ? s.Value : null;

    /// <summary>
    /// Read a <c>key: { name1: true, name2: false }</c> mapping; only
    /// names whose value is <c>true</c> are returned (rAthena treats
    /// false the same as omitted).
    /// </summary>
    private static HashSet<string> ReadFlagMap(YamlMappingNode parent, string key)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!parent.Children.TryGetValue(key, out var v) || v is not YamlMappingNode m) return set;
        foreach (var (k, val) in m.Children)
        {
            if (k is YamlScalarNode ks && val is YamlScalarNode vs
                && string.Equals(vs.Value, "true", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(ks.Value))
            {
                set.Add(ks.Value!);
            }
        }
        return set;
    }

    private static HashSet<PcPermission> ReadPermissions(YamlMappingNode parent)
    {
        var set = new HashSet<PcPermission>();
        var raw = ReadFlagMap(parent, "Permissions");
        foreach (var s in raw)
        {
            if (PcPermissionExtensions.TryParse(s, out var perm)) set.Add(perm);
        }
        return set;
    }

    private sealed record RawGroup(
        int Id,
        string Name,
        int Level,
        bool LogCommands,
        HashSet<string> Commands,
        HashSet<string> CharCommands,
        HashSet<PcPermission> Permissions,
        HashSet<string> Inherits);
}
