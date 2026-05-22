using System.Globalization;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;

namespace Map.Server.Mob;

/// <summary>
/// Loads rAthena <c>db/mob_chat_db.yml</c> into <see cref="IMobChatDb"/>.
/// Mirrors the rAthena YAML schema (mob.cpp:6312, MobChatDatabase):
/// <code>
/// Body:
///   - Id: 1
///     Color: 0xFFFFFF        # default 0xFF0000 when absent
///     Dialog: "Some message"
/// </code>
///
/// <para>Missing file = no rows loaded; the canonical
/// surface stays alive but <see cref="IClifWireService.MobChat"/>
/// just won't have anything to broadcast. Logs at info level so the
/// boot transcript shows why mob chat lines are silent.</para>
///
/// <para>T5.1b: this is the YAML→runtime path. The Tier 1 convention
/// is YAML → SQL → repo → runtime, but mob_chat_db is a single
/// 57-row lookup table that already fits in memory. Promoting to
/// a full SQL table is queued as a follow-up; ingest-from-YAML keeps
/// the loader honest until then.</para>
/// </summary>
public sealed class MobChatYmlLoader
{
    /// <summary>rAthena default color (mob.cpp:6334).</summary>
    public const uint DefaultColor = 0xFF0000;

    private readonly ILogger<MobChatYmlLoader> _logger;

    public MobChatYmlLoader(ILogger<MobChatYmlLoader> logger) => _logger = logger;

    /// <summary>
    /// Read rows from <paramref name="yamlPath"/> and stuff them into
    /// <paramref name="db"/>. Returns the number of rows added; 0 on
    /// missing / unparseable / empty file. Safe to call more than once
    /// (overwrite semantics, mirrors rAthena <c>/reloadmobdb</c>).
    /// </summary>
    public int Load(string yamlPath, IMobChatDb db)
    {
        if (!File.Exists(yamlPath))
        {
            _logger.LogInformation(
                "mob_chat_db.yml not found at {Path} — mob chat broadcasts disabled",
                yamlPath);
            return 0;
        }
        using var reader = File.OpenText(yamlPath);
        var stream = new YamlStream();
        try { stream.Load(reader); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mob_chat_db.yml parse error — mob chat broadcasts disabled");
            return 0;
        }
        if (stream.Documents.Count == 0) return 0;
        if (stream.Documents[0].RootNode is not YamlMappingNode root) return 0;
        if (!root.Children.TryGetValue("Body", out var bodyNode) || bodyNode is not YamlSequenceNode body)
            return 0;

        var added = 0;
        foreach (var item in body.Children.OfType<YamlMappingNode>())
        {
            if (!TryInt(item, "Id", out var id)) continue;
            if (!TryString(item, "Dialog", out var dialog) || string.IsNullOrEmpty(dialog)) continue;
            var color = TryColor(item, "Color", out var c) ? c : DefaultColor;
            db.Add(new MobChatRow(id, color, dialog));
            added++;
        }
        _logger.LogInformation(
            "mob_chat_db.yml: loaded {Count} chat rows from {Path}", added, yamlPath);
        return added;
    }

    private static bool TryInt(YamlMappingNode node, string key, out int value)
    {
        value = 0;
        if (!node.Children.TryGetValue(key, out var v) || v is not YamlScalarNode s) return false;
        return int.TryParse(s.Value, out value);
    }

    private static bool TryString(YamlMappingNode node, string key, out string value)
    {
        value = string.Empty;
        if (!node.Children.TryGetValue(key, out var v) || v is not YamlScalarNode s) return false;
        value = s.Value ?? string.Empty;
        return true;
    }

    /// <summary>
    /// Parse a color value. rAthena writes <c>0xRRGGBB</c>; YamlDotNet
    /// represents the literal as a string. Accept the <c>0x</c> prefix
    /// (or omit it) and parse as hex. Falls back to <see cref="DefaultColor"/>
    /// on any parse failure.
    /// </summary>
    private static bool TryColor(YamlMappingNode node, string key, out uint value)
    {
        value = DefaultColor;
        if (!node.Children.TryGetValue(key, out var v) || v is not YamlScalarNode s) return false;
        var text = (s.Value ?? string.Empty).Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            text = text.Substring(2);
        return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }
}
