namespace Tools.RathenaImporter;

/// <summary>
/// rAthena warp-line format:
/// <code>
///   srcmap,srcx,srcy,srcdir &lt;TAB&gt; warp|warp2 &lt;TAB&gt;
///   name &lt;TAB&gt; xs,ys,destmap,destx,desty
/// </code>
/// Lines with anything other than <c>warp</c> / <c>warp2</c> in column
/// 2 (most commonly <c>script</c> with a body) are skipped.
/// </summary>
public sealed record WarpRow(
    string SrcMap, short SrcX, short SrcY, byte SrcDir,
    string WarpType, string Name,
    short SpanXs, short SpanYs,
    string DstMap, short DstX, short DstY);

public static class WarpParser
{
    public static IEnumerable<WarpRow> ParseFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var row = TryParse(raw);
            if (row != null) yield return row;
        }
    }

    /// <summary>
    /// Parse a single line. Returns null when the line is a comment,
    /// blank, malformed, or carries a script body (we only emit
    /// declarative warps).
    /// </summary>
    public static WarpRow? TryParse(string raw)
    {
        var line = raw.TrimStart();
        if (line.Length == 0 || line.StartsWith("//")) return null;

        // rAthena uses tabs as the column separator. Some files mix tabs
        // and spaces; split on any whitespace run between columns.
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return null;

        var directive = parts[1].Trim().ToLowerInvariant();
        if (directive != "warp" && directive != "warp2") return null;

        var src = parts[0].Split(',');
        if (src.Length != 4) return null;

        var detail = parts[3].Split(',');
        if (detail.Length != 5) return null;

        if (!short.TryParse(src[1], out var sx) ||
            !short.TryParse(src[2], out var sy) ||
            !byte.TryParse(src[3], out var sd) ||
            !short.TryParse(detail[0], out var xs) ||
            !short.TryParse(detail[1], out var ys) ||
            !short.TryParse(detail[3], out var dx) ||
            !short.TryParse(detail[4], out var dy))
            return null;

        return new WarpRow(
            SrcMap: src[0],
            SrcX: sx, SrcY: sy, SrcDir: sd,
            WarpType: directive,
            Name: parts[2].Trim(),
            SpanXs: xs, SpanYs: ys,
            DstMap: detail[2],
            DstX: dx, DstY: dy);
    }
}
