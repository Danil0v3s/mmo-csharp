using System.Text.RegularExpressions;

namespace Tools.ItemScriptConvert;

/// <summary>
/// Regex-based readers for the SQL seed files. Hermetic — no MySQL
/// connection needed; the seed-file shape is stable enough that
/// match-and-extract beats spinning up a SQL parser.
///
/// Two un-escape passes apply in order: <c>\\</c> → <c>\</c> (SQL stores
/// backslashes doubled), then <c>''</c> → <c>'</c> (SQL escapes a single
/// quote inside a quoted literal by doubling it). Order matters — flipping
/// the order double-decodes embedded backslashes.
/// </summary>
internal static class SeedReader
{
    /// <summary>What kind of rAthena script column produced this script.</summary>
    public enum ScriptKind
    {
        /// <summary>item_db.script — fires onUse for usable items, onEquip for gear.</summary>
        Script,
        /// <summary>item_db.equip_script — fires on equip success.</summary>
        EquipScript,
        /// <summary>item_db.unequip_script — fires on unequip.</summary>
        UnequipScript,
    }

    public readonly record struct ItemRow(int Id, ScriptKind Kind, string Body);
    public readonly record struct ComboRow(int ComboId, string Body, IReadOnlyList<string> Members);

    /// <summary>
    /// Stream every script column out of an item_db_*.sql seed file.
    /// Yields one ItemRow per (id, script-kind) pair — an item with both
    /// a script and an equip_script emits two rows.
    ///
    /// <para>
    /// Hand-rolled extractor (not regex) because item scripts contain
    /// <c>);</c> sequences inside quoted bodies (e.g. <c>'... getequiprefinerycnt(EQI_HAND_R); bonus ...'</c>),
    /// which a naive regex stops at. The walker tracks SQL string state
    /// — backslash escapes and doubled-single-quote escapes — so the
    /// matching <c>);</c> outside any string ends each row reliably.
    /// </para>
    /// </summary>
    public static IEnumerable<ItemRow> ReadItems(string seedPath)
    {
        if (!File.Exists(seedPath))
            throw new FileNotFoundException($"item-db seed not found at {seedPath}");
        var content = File.ReadAllText(seedPath);

        const string prefix = "REPLACE INTO `item_db` (";
        var i = 0;
        while ((i = content.IndexOf(prefix, i, StringComparison.Ordinal)) >= 0)
        {
            i += prefix.Length;
            var colsStart = i;
            var colsEnd = content.IndexOf(')', colsStart);
            if (colsEnd < 0) break;
            // colsEnd points at the closing `)` of the column list; the
            // value-tuple marker starts one byte past it.
            const string valuesMarker = " VALUES (";
            if (!content.AsSpan(colsEnd + 1).StartsWith(valuesMarker)) { i = colsEnd; continue; }
            var valsStart = colsEnd + 1 + valuesMarker.Length;
            var valsEnd = FindValuesEnd(content, valsStart);
            if (valsEnd < 0) break;

            var cols = ParseColumnList(content[colsStart..colsEnd]);
            var vals = ParseValueTuple(content[valsStart..valsEnd]);
            i = valsEnd + 2; // skip ");"

            if (cols.Count != vals.Count) continue;
            var idIdx = cols.IndexOf("id");
            if (idIdx < 0) continue;
            if (!int.TryParse(vals[idIdx], out var id)) continue;

            foreach (var (colName, kind) in ScriptColumns)
            {
                var idx = cols.IndexOf(colName);
                if (idx < 0) continue;
                var raw = vals[idx];
                if (string.IsNullOrEmpty(raw) || raw == "NULL") continue;
                if (raw.Length < 2 || raw[0] != '\'' || raw[^1] != '\'') continue;
                var body = UnEscapeSqlString(raw[1..^1]);
                if (string.IsNullOrWhiteSpace(body)) continue;
                yield return new ItemRow(id, kind, body);
            }
        }
    }

    /// <summary>
    /// Walk forward from the first byte after <c>VALUES (</c> and return
    /// the index of the matching closing <c>)</c> (the one followed by
    /// <c>;</c>). Tracks SQL string state so <c>);</c> sequences inside
    /// quoted bodies don't trigger early termination.
    /// </summary>
    private static int FindValuesEnd(string s, int start)
    {
        var inString = false;
        for (var i = start; i < s.Length; i++)
        {
            var c = s[i];
            if (inString)
            {
                if (c == '\\' && i + 1 < s.Length) { i++; continue; }
                if (c == '\'')
                {
                    // Doubled '' is an embedded apostrophe, not a terminator.
                    if (i + 1 < s.Length && s[i + 1] == '\'') { i++; continue; }
                    inString = false;
                }
                continue;
            }
            if (c == '\'') { inString = true; continue; }
            if (c == ')' && i + 1 < s.Length && s[i + 1] == ';') return i;
        }
        return -1;
    }

    private static readonly (string Col, ScriptKind Kind)[] ScriptColumns =
    {
        ("script", ScriptKind.Script),
        ("equip_script", ScriptKind.EquipScript),
        ("unequip_script", ScriptKind.UnequipScript),
    };

    /// <summary>
    /// Stream every combo row + its member list out of seed_item_combos.sql.
    /// The seed alternates:
    ///   REPLACE INTO `item_combo_db`        (combo_id, script) VALUES (...);
    ///   REPLACE INTO `item_combo_member_db` (combo_id, member_item_aegis) VALUES (...);
    /// We walk the file twice — once to collect combo bodies, once to collect
    /// members — and join by combo_id. Member-list order is preserved.
    /// </summary>
    public static IEnumerable<ComboRow> ReadCombos(string seedPath)
    {
        if (!File.Exists(seedPath))
            throw new FileNotFoundException($"item-combos seed not found at {seedPath}");
        var content = File.ReadAllText(seedPath);

        var bodyRx = new Regex(
            @"REPLACE INTO `item_combo_db` \(`combo_id`,`script`\) VALUES \((\d+),'((?:''|\\.|[^'])*)'\);",
            RegexOptions.Compiled | RegexOptions.Singleline);
        var memberRx = new Regex(
            @"REPLACE INTO `item_combo_member_db` \(`combo_id`,`member_item_aegis`\) VALUES \((\d+),'((?:''|\\.|[^'])*)'\);",
            RegexOptions.Compiled);

        var bodies = new Dictionary<int, string>();
        foreach (Match m in bodyRx.Matches(content))
        {
            var id = int.Parse(m.Groups[1].Value);
            bodies[id] = UnEscapeSqlString(m.Groups[2].Value);
        }

        var members = new Dictionary<int, List<string>>();
        foreach (Match m in memberRx.Matches(content))
        {
            var id = int.Parse(m.Groups[1].Value);
            var aegis = UnEscapeSqlString(m.Groups[2].Value);
            if (!members.TryGetValue(id, out var list))
                members[id] = list = new List<string>();
            list.Add(aegis);
        }

        // Yield in combo-id order so generated bucket files have stable
        // contents across runs.
        foreach (var id in bodies.Keys.OrderBy(k => k))
        {
            var memberList = members.TryGetValue(id, out var ms)
                ? (IReadOnlyList<string>)ms
                : Array.Empty<string>();
            yield return new ComboRow(id, bodies[id], memberList);
        }
    }

    /// <summary>
    /// Split a `col1`,`col2`,... list into ["col1","col2",...].
    /// </summary>
    private static List<string> ParseColumnList(string raw)
    {
        var cols = new List<string>();
        foreach (var seg in raw.Split(','))
        {
            var trimmed = seg.Trim().Trim('`');
            if (trimmed.Length > 0) cols.Add(trimmed);
        }
        return cols;
    }

    /// <summary>
    /// Split a VALUES tuple into its individual values. Handles quoted
    /// strings (including escaped quotes and backslashes), numbers,
    /// NULL, and booleans. Commas inside quoted strings don't split.
    /// </summary>
    private static List<string> ParseValueTuple(string raw)
    {
        var result = new List<string>();
        var i = 0;
        while (i < raw.Length)
        {
            // Skip leading whitespace.
            while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
            if (i >= raw.Length) break;

            int start = i;
            if (raw[i] == '\'')
            {
                // Quoted literal — walk until the matching '. Doubled ''
                // is an escape, backslash escapes the next char.
                i++;
                while (i < raw.Length)
                {
                    if (raw[i] == '\\' && i + 1 < raw.Length) { i += 2; continue; }
                    if (raw[i] == '\'')
                    {
                        if (i + 1 < raw.Length && raw[i + 1] == '\'') { i += 2; continue; }
                        i++;
                        break;
                    }
                    i++;
                }
                result.Add(raw[start..i]);
            }
            else
            {
                // Unquoted — number, NULL, true/false, etc. Read until comma.
                while (i < raw.Length && raw[i] != ',') i++;
                result.Add(raw[start..i].Trim());
            }
            // Consume the trailing comma if present.
            while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
            if (i < raw.Length && raw[i] == ',') i++;
        }
        return result;
    }

    /// <summary>
    /// Single-pass MySQL string decoder. The combo seed gets away with a
    /// cascaded two-Replace pass because its bodies never contain
    /// backslash escapes other than <c>\\</c>; the item seeds (especially
    /// the usable potions) carry <c>\n</c> / <c>\t</c> / <c>\r</c> as
    /// C-style escapes that need a proper walker — replacing in two
    /// passes either misses them or double-decodes embedded backslashes.
    ///
    /// <para>
    /// Recognised escapes follow MySQL's grammar:
    /// <c>\\</c> → <c>\</c>, <c>\'</c> → <c>'</c>, <c>\"</c> → <c>"</c>,
    /// <c>\n</c> → LF, <c>\r</c> → CR, <c>\t</c> → TAB, <c>\0</c> → NUL,
    /// <c>\b</c> → BS, <c>\Z</c> → ASCII 26, <c>\%</c> / <c>\_</c> kept
    /// verbatim (LIKE-pattern wildcards). Unknown escapes pass through
    /// as the second char (matches MySQL behavior). Doubled <c>''</c>
    /// outside any backslash run decodes to <c>'</c>.
    /// </para>
    /// </summary>
    private static string UnEscapeSqlString(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                var n = s[i + 1];
                sb.Append(n switch
                {
                    '\\' => '\\',
                    '\'' => '\'',
                    '"'  => '"',
                    'n'  => '\n',
                    'r'  => '\r',
                    't'  => '\t',
                    '0'  => '\0',
                    'b'  => '\b',
                    'Z'  => (char)26,
                    _    => n, // pass-through for \% \_ and any unknown
                });
                i++;
                continue;
            }
            if (c == '\'' && i + 1 < s.Length && s[i + 1] == '\'')
            {
                sb.Append('\'');
                i++;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
