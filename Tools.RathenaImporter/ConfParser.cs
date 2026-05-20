using System.Text;

namespace Tools.RathenaImporter;

/// <summary>
/// Minimal parser for rAthena's HOCON-ish .conf format. Handles:
/// <list type="bullet">
///   <item><c>// line comment</c></item>
///   <item><c>/* block comment */</c></item>
///   <item><c>key: value</c> (int / hex / string / bare identifier)</item>
///   <item><c>key: { nested ... }</c></item>
///   <item><c>import: "path"</c> — kept as an array of paths, not followed</item>
/// </list>
///
/// Comments immediately preceding a key are captured as that key's
/// description and lifted into JSON Schema. Keys lose their type
/// information past the conversion step — the JSON Schema is the
/// reference for typing the JSON file.
/// </summary>
public static class ConfParser
{
    public sealed record ConfKey(string Name, string? Value, List<ConfKey> Children, string? Description);

    /// <summary>
    /// Parse a single .conf file into a flat or nested ConfKey tree.
    /// The root has Name="" and Children populated.
    /// </summary>
    public static ConfKey Parse(string path)
    {
        var text = File.ReadAllText(path);
        var pos = 0;
        var root = new ConfKey("", null, new List<ConfKey>(), null);
        ParseBlock(text, ref pos, root);
        return root;
    }

    private static void ParseBlock(string text, ref int pos, ConfKey parent)
    {
        var lastDescription = new StringBuilder();
        while (pos < text.Length)
        {
            SkipWhitespace(text, ref pos);
            if (pos >= text.Length) break;
            var c = text[pos];
            if (c == '/')
            {
                var comment = TryReadComment(text, ref pos);
                if (comment != null)
                {
                    if (lastDescription.Length > 0) lastDescription.AppendLine();
                    lastDescription.Append(comment);
                }
                continue;
            }
            if (c == '}')
            {
                pos++;
                return;
            }

            // key
            var keyStart = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_' || text[pos] == '.' || text[pos] == '-')) pos++;
            if (pos == keyStart) { pos++; continue; }
            var key = text.Substring(keyStart, pos - keyStart);

            SkipWhitespace(text, ref pos);
            if (pos < text.Length && text[pos] == ':')
            {
                pos++; // consume :
                SkipWhitespace(text, ref pos);
                if (pos < text.Length && text[pos] == '{')
                {
                    pos++; // consume {
                    var nested = new ConfKey(key, null, new List<ConfKey>(),
                        lastDescription.Length > 0 ? lastDescription.ToString().Trim() : null);
                    lastDescription.Clear();
                    ParseBlock(text, ref pos, nested);
                    parent.Children.Add(nested);
                }
                else
                {
                    var value = ReadValue(text, ref pos);
                    parent.Children.Add(new ConfKey(key, value, new List<ConfKey>(),
                        lastDescription.Length > 0 ? lastDescription.ToString().Trim() : null));
                    lastDescription.Clear();
                }
            }
            // else: bare token, skip silently
        }
    }

    private static string ReadValue(string text, ref int pos)
    {
        SkipInlineSpace(text, ref pos);
        if (pos >= text.Length) return "";
        if (text[pos] == '"')
        {
            // quoted string
            var sb = new StringBuilder();
            pos++; // consume opening "
            while (pos < text.Length && text[pos] != '"')
            {
                if (text[pos] == '\\' && pos + 1 < text.Length) { sb.Append(text[pos + 1]); pos += 2; continue; }
                sb.Append(text[pos]);
                pos++;
            }
            if (pos < text.Length) pos++; // consume closing "
            return sb.ToString();
        }
        var start = pos;
        while (pos < text.Length && text[pos] != '\n' && text[pos] != '\r' && text[pos] != '/')
        {
            pos++;
        }
        // trim trailing inline-comment ("//")
        return text.Substring(start, pos - start).Trim();
    }

    private static string? TryReadComment(string text, ref int pos)
    {
        if (pos + 1 >= text.Length) { pos++; return null; }
        if (text[pos + 1] == '/')
        {
            pos += 2;
            var start = pos;
            while (pos < text.Length && text[pos] != '\n') pos++;
            var line = text.Substring(start, pos - start).Trim();
            return line.Length > 0 ? line : null;
        }
        if (text[pos + 1] == '*')
        {
            pos += 2;
            var start = pos;
            while (pos + 1 < text.Length && !(text[pos] == '*' && text[pos + 1] == '/')) pos++;
            var block = pos > start ? text.Substring(start, pos - start) : "";
            if (pos + 1 < text.Length) pos += 2;
            return Sanitize(block);
        }
        pos++;
        return null;
    }

    private static string Sanitize(string s)
    {
        var lines = s.Split('\n');
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.Trim().TrimStart('*').Trim();
            if (trimmed.Length == 0) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(trimmed);
        }
        return sb.ToString();
    }

    private static void SkipWhitespace(string text, ref int pos)
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
    }

    private static void SkipInlineSpace(string text, ref int pos)
    {
        while (pos < text.Length && (text[pos] == ' ' || text[pos] == '\t')) pos++;
    }
}
