namespace Tools.RathenaImporter;

/// <summary>
/// rAthena mob-spawn line:
/// <code>
///   map,x,y,xs,ys &lt;TAB&gt; monster|boss_monster &lt;TAB&gt;
///   name &lt;TAB&gt; mob_id,amount,delay1,delay2[,event[,size[,ai]]]
/// </code>
/// </summary>
public sealed record MobSpawnRow(
    string MapName, short CenterX, short CenterY, short SpanXs, short SpanYs,
    bool IsBoss, string DisplayName,
    int MobId, int Amount, int Delay1, int Delay2,
    string EventLabel, int Size, int Ai);

public static class MobSpawnParser
{
    public static IEnumerable<MobSpawnRow> ParseFile(string path)
    {
        foreach (var raw in File.ReadAllLines(path))
        {
            var row = TryParse(raw);
            if (row != null) yield return row;
        }
    }

    public static MobSpawnRow? TryParse(string raw)
    {
        var line = raw.TrimStart();
        if (line.Length == 0 || line.StartsWith("//")) return null;

        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4) return null;

        var directive = parts[1].Trim().ToLowerInvariant();
        if (directive != "monster" && directive != "boss_monster") return null;

        // Location is either "map,x,y" (anywhere on map; rAthena uses
        // 0,0 then for the "random anywhere" case) or "map,x,y,xs,ys"
        // (centered spawn area with half-extent).
        var loc = parts[0].Split(',');
        if (loc.Length != 3 && loc.Length != 5) return null;

        var detail = parts[3].Split(',');
        if (detail.Length < 2) return null;

        if (!short.TryParse(loc[1], out var cx) ||
            !short.TryParse(loc[2], out var cy) ||
            !int.TryParse(detail[0], out var mobId) ||
            !int.TryParse(detail[1], out var amount))
            return null;

        short xs = 0, ys = 0;
        if (loc.Length == 5 && (!short.TryParse(loc[3], out xs) || !short.TryParse(loc[4], out ys)))
            return null;

        var delay1 = detail.Length > 2 && int.TryParse(detail[2], out var d1) ? d1 : 0;
        var delay2 = detail.Length > 3 && int.TryParse(detail[3], out var d2) ? d2 : 0;
        var ev = detail.Length > 4 ? detail[4].Trim() : string.Empty;
        var size = detail.Length > 5 && int.TryParse(detail[5], out var s) ? s : 0;
        var ai = detail.Length > 6 && int.TryParse(detail[6], out var a) ? a : 0;

        return new MobSpawnRow(
            MapName: loc[0],
            CenterX: cx, CenterY: cy,
            SpanXs: xs, SpanYs: ys,
            IsBoss: directive == "boss_monster",
            DisplayName: parts[2].Trim(),
            MobId: mobId, Amount: amount,
            Delay1: delay1, Delay2: delay2,
            EventLabel: ev, Size: size, Ai: ai);
    }
}
