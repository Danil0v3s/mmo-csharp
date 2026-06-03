using System;
using System.Collections.Generic;
using System.Linq;

namespace Map.Server.Status;

/// <summary>
/// COMBAT-81 — resolves the <see cref="BattleRace2"/> axis from both data sources that feed it:
/// the mob_db <c>RaceGroups</c> dictionary (a mob's race2 set) and the <c>RC2_*</c> script tokens
/// (<c>bonus2 bAddRace2, RC2_X, n</c>). Both normalize to the same enum via
/// case-/underscore-insensitive matching of the enum member names.
/// </summary>
public static class Race2Map
{
    private static readonly IReadOnlyDictionary<string, BattleRace2> ByNormalized =
        Enum.GetValues<BattleRace2>()
            .Where(r => r != BattleRace2.None && r != BattleRace2.Max)
            .ToDictionary(r => Normalize(r.ToString()), r => r);

    private static string Normalize(string s) => s.Replace("_", string.Empty).ToLowerInvariant();

    /// <summary>Map a mob_db RaceGroups key (e.g. "Goblin", "OghAtkDef") to its race2 id.</summary>
    public static BattleRace2 FromGroupKey(string key)
        => ByNormalized.GetValueOrDefault(Normalize(key), BattleRace2.None);

    /// <summary>Map an <c>RC2_*</c> script token (e.g. "RC2_GOBLIN", "RC2_OGH_ATK_DEF") to its race2 id.</summary>
    public static BattleRace2 FromToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return BattleRace2.None;
        var stem = token.StartsWith("RC2_", StringComparison.OrdinalIgnoreCase) ? token[4..] : token;
        return ByNormalized.GetValueOrDefault(Normalize(stem), BattleRace2.None);
    }

    /// <summary>The set of race2 ids a mob belongs to (rAthena <c>status_get_race2</c>).</summary>
    public static IReadOnlyList<BattleRace2> FromRaceGroups(IReadOnlyDictionary<string, bool>? groups)
    {
        if (groups == null || groups.Count == 0) return Array.Empty<BattleRace2>();
        List<BattleRace2>? list = null;
        foreach (var (key, on) in groups)
        {
            if (!on) continue;
            var r2 = FromGroupKey(key);
            if (r2 == BattleRace2.None) continue;
            (list ??= new List<BattleRace2>()).Add(r2);
        }
        return (IReadOnlyList<BattleRace2>?)list ?? Array.Empty<BattleRace2>();
    }
}
