using System.Text;
using Map.Server.Inventory.Script;

namespace Tools.ItemScriptConvert;

/// <summary>
/// Translates a parsed rAthena script body to a JS function-literal that
/// the TypeScript bundle can pass as <c>onUse</c> / <c>onEquip</c> /
/// <c>onUnequip</c> / <c>onActive</c>. Catches parse failures and
/// records them as comments so the generated .ts compiles even when
/// individual scripts can't translate.
/// </summary>
internal static class TsEmitter
{
    public sealed record EmitResult(string FunctionBody, string? SkipReason)
    {
        public bool Ok => SkipReason == null;
    }

    /// <summary>
    /// Translate a rAthena script body to a JS function expression body.
    /// Returns just the body lines (no <c>function</c> keyword, no braces)
    /// so the caller can wrap it inside an arrow or method literal.
    /// </summary>
    public static EmitResult TranslateBody(string rathenaScript)
    {
        if (string.IsNullOrWhiteSpace(rathenaScript))
            return new EmitResult("", null);

        try
        {
            var ast = RathenaScriptParser.Parse(rathenaScript);
            var js = RathenaToJsTranslator.Translate(ast, receiverName: "ctx");
            return new EmitResult(js, null);
        }
        catch (ScriptParseException ex)
        {
            return new EmitResult("", $"parse: {OneLine(ex.Message)}");
        }
        catch (Exception ex)
        {
            return new EmitResult("", $"translate: {ex.GetType().Name}: {OneLine(ex.Message)}");
        }
    }

    /// <summary>
    /// Indent a multi-line body to fit nested inside a TypeScript method.
    /// Strips trailing whitespace per line for clean diffs.
    /// </summary>
    public static string Indent(string body, int spaces)
    {
        if (string.IsNullOrEmpty(body)) return "";
        var prefix = new string(' ', spaces);
        var sb = new StringBuilder();
        var first = true;
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.TrimEnd('\r', ' ', '\t');
            if (!first) sb.Append('\n');
            first = false;
            if (line.Length > 0) sb.Append(prefix).Append(line);
        }
        return sb.ToString();
    }

    /// <summary>Squash multi-line strings to a single line for comments.</summary>
    private static string OneLine(string s) =>
        s.Replace("\r", "").Replace("\n", " ").Trim();

    /// <summary>
    /// Encode a string for embedding inside a TypeScript double-quoted
    /// literal. Aegis names are typically alnum + underscore but rAthena
    /// has special characters (apostrophes in "Goibne's_Armor") so we
    /// escape defensively.
    /// </summary>
    public static string TsString(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
