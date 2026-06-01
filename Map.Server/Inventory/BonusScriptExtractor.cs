using System.Text.RegularExpressions;
using Map.Server.Status;

namespace Map.Server.Inventory;

/// <summary>
/// Regex-based extractor for the common <c>bonus</c> / <c>bonus2</c>
/// / <c>bonus3</c> patterns in rAthena's item-script DSL. Populates
/// an <see cref="EquipBonusBundle"/> from each equipped item's
/// <c>script</c> column in <c>item_db</c>.
///
/// <para>This is intentionally a regex pass, not a full DSL parser.
/// Coverage focuses on the static patterns (flat numeric arguments)
/// that account for ~90 % of cards + most armor / weapon attributes.
/// Anything dynamic (<c>getrefine()</c>, <c>if (BaseLevel &gt; …)</c>,
/// <c>callfunc</c>) is skipped silently — the script engine port
/// handles those later. Skipped patterns leave the bundle slot at 0,
/// not a wrong number, so the bias is "miss" not "lie".</para>
///
/// <para><b>Patterns covered (renewal):</b></para>
/// <list type="bullet">
///   <item><c>bonus bAtk, N;</c> → <c>FlatAtk += N</c></item>
///   <item><c>bonus bMatk, N;</c> → <c>FlatMatk += N</c></item>
///   <item><c>bonus bCritical, N;</c></item>
///   <item><c>bonus bHit, N;</c></item>
///   <item><c>bonus bFlee, N;</c></item>
///   <item><c>bonus bMaxHP, N;</c></item>
///   <item><c>bonus bMaxSP, N;</c></item>
///   <item><c>bonus bMaxHPrate, N;</c></item>
///   <item><c>bonus bMaxSPrate, N;</c></item>
///   <item><c>bonus bLongAtkRate, N;</c></item>
///   <item><c>bonus bShortAtkRate, N;</c></item>
///   <item><c>bonus bCritAtkRate, N;</c></item>
///   <item><c>bonus bAspd, N;</c> / <c>bonus bAspdRate, N;</c></item>
///   <item><c>bonus bVariableCastrate, N;</c></item>
///   <item><c>bonus bFixedCastrate, N;</c></item>
///   <item><c>bonus bDelayrate, N;</c></item>
///   <item><c>bonus2 bAddRace, RC_X, N;</c> → <c>AddRace[X] += N</c></item>
///   <item><c>bonus2 bSubRace, RC_X, N;</c></item>
///   <item><c>bonus2 bAddEle, Ele_X, N;</c></item>
///   <item><c>bonus2 bSubEle, Ele_X, N;</c></item>
///   <item><c>bonus2 bAddSize, Size_X, N;</c></item>
///   <item><c>bonus2 bSubSize, Size_X, N;</c></item>
///   <item><c>bonus2 bAddClass, Class_X, N;</c></item>
///   <item><c>bonus2 bSubClass, Class_X, N;</c></item>
///   <item><c>bonus2 bHPDrainRate, N, P;</c> / <c>bSPDrainRate, N, P;</c></item>
/// </list>
/// </summary>
public static class BonusScriptExtractor
{
    // Single-statement matchers. The DSL uses commas as separators
    // and trailing semicolons. We accept whitespace + optional
    // negative numbers.

    private static readonly Regex BonusFlat = new(
        @"bonus\s+b(?<key>[A-Za-z]+)\s*,\s*(?<v>-?\d+)\s*;",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Bonus2Indexed = new(
        @"bonus2\s+b(?<key>[A-Za-z]+)\s*,\s*(?<idx>[A-Za-z_][A-Za-z_0-9]*)\s*,\s*(?<v>-?\d+)\s*;",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static void Apply(string? script, EquipBonusBundle bundle)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        foreach (Match m in BonusFlat.Matches(script))
        {
            var key = m.Groups["key"].Value;
            if (!int.TryParse(m.Groups["v"].Value, out var v)) continue;
            ApplyFlat(bundle, key, v);
        }

        foreach (Match m in Bonus2Indexed.Matches(script))
        {
            var key = m.Groups["key"].Value;
            var idxToken = m.Groups["idx"].Value;
            if (!int.TryParse(m.Groups["v"].Value, out var v)) continue;
            ApplyIndexed(bundle, key, idxToken, v);
        }
    }

    /// <summary>
    /// Apply a single flat bonus (one indexed by key only) to the bundle.
    /// Exposed for the V8 host (<see cref="Map.Server.Inventory.Script.ScriptedBonusHost"/>)
    /// so JS-translated scripts route through the same keyword switch as
    /// the regex extractor — no parallel keyword tables to drift apart.
    /// </summary>
    internal static void ApplyFlatBonus(EquipBonusBundle b, string key, int v)
        => ApplyFlat(b, key, v);

    /// <summary>
    /// Apply a single indexed bonus (key + RC_/Ele_/Size_/Class_ token).
    /// Same intent as <see cref="ApplyFlatBonus"/>.
    /// </summary>
    internal static void ApplyIndexedBonus(EquipBonusBundle b, string key, string idxToken, int v)
        => ApplyIndexed(b, key, idxToken, v);

    private static void ApplyFlat(EquipBonusBundle b, string key, int v)
    {
        // Case-insensitive comparison; we lower-case the key for the switch.
        switch (key.ToLowerInvariant())
        {
            case "atk":            b.FlatAtk += v; break;
            case "matk":           b.FlatMatk += v; break;
            case "critical":       b.FlatCritical += v; break;
            case "hit":            b.FlatHit += v; break;
            case "flee":           b.FlatFlee += v; break;
            case "aspd":           b.FlatAspd += v; break;
            case "aspdrate":       b.FlatAspdRate += v; break;
            case "maxhp":          b.FlatMaxHp += v; break;
            case "maxsp":          b.FlatMaxSp += v; break;
            case "maxhprate":      b.MaxHpRate += v; break;
            case "maxsprate":      b.MaxSpRate += v; break;
            case "longatkrate":    b.LongAtkRate += v; break;
            case "shortatkrate":   b.ShortAtkRate += v; break;
            case "critatkrate":    b.CritAtkRate += v; break;
            case "variablecastrate": b.VarCastRate += v; break;
            case "fixedcastrate":  b.FixCastRate += v; break;
            case "delayrate":      b.DelayRate += v; break;
            // COMBAT-06 — damage / defense rate bonuses.
            case "atkrate":        b.AtkRate += v; break;
            case "matkrate":       b.MatkRate += v; break;
            case "def":            b.FlatDef += v; break;
            case "mdef":           b.FlatMdef += v; break;
            case "defrate":        b.DefRate += v; break;
            case "mdefrate":       b.MdefRate += v; break;
            // Primary + trait param bonuses (rAthena pc_bonus SP_STR..SP_LUK
            // / SP_POW..SP_CRT). Captured into the bundle; the base→final
            // stat layering that adds them to the displayed/derived stat
            // lands in COMBAT-10 (cannot fold idempotently today — see the
            // bundle field doc + COMBAT-10).
            case "str":  b.Str += v; break;
            case "agi":  b.Agi += v; break;
            case "vit":  b.Vit += v; break;
            case "int":  b.IntStat += v; break;
            case "dex":  b.Dex += v; break;
            case "luk":  b.Luk += v; break;
            case "pow":  b.Pow += v; break;
            case "sta":  b.Sta += v; break;
            case "wis":  b.Wis += v; break;
            case "spl":  b.Spl += v; break;
            case "con":  b.Con += v; break;
            case "crt":  b.Crt += v; break;
            // bAllStats / bAgiVit / bAgiDexStr etc. (composite keys) and
            // the remaining long tail land with COMBAT-06.
        }
    }

    private static void ApplyIndexed(EquipBonusBundle b, string key, string idxToken, int v)
    {
        switch (key.ToLowerInvariant())
        {
            case "addrace": Add(b.AddRace, ParseRace(idxToken), v); break;
            case "subrace": Add(b.SubRace, ParseRace(idxToken), v); break;
            case "addele":  Add(b.AddEle, ParseElement(idxToken), v); break;
            case "subele":  Add(b.SubEle, ParseElement(idxToken), v); break;
            case "addsize": Add(b.AddSize, ParseSize(idxToken), v); break;
            case "subsize": Add(b.SubSize, ParseSize(idxToken), v); break;
            case "addclass": Add(b.AddClass, ParseClass(idxToken), v); break;
            case "subclass": Add(b.SubClass, ParseClass(idxToken), v); break;
            case "hpdrainrate": b.DrainHpRate += v; break;
            case "spdrainrate": b.DrainSpRate += v; break;
            // Wave 65 — coma proc tables.
            case "comaclass": AddShort(b.ComaClass, ParseClass(idxToken), (short)v); break;
            case "comarace":  AddShort(b.ComaRace,  ParseRace(idxToken),  (short)v); break;
            // bAddRace2, bMagicAddRace, bIgnoreDefRate, ... — many
            // more layered options. Leave at 0 until the consumer
            // ports.
        }
    }

    private static void AddShort(short[] arr, int idx, short v)
    {
        if (idx < 0 || idx >= arr.Length) return;
        // Saturated add to short range — rate is in permille so the
        // largest sane value is ~10 000, well within short.MaxValue.
        var sum = arr[idx] + v;
        if (sum > short.MaxValue) sum = short.MaxValue;
        if (sum < short.MinValue) sum = short.MinValue;
        arr[idx] = (short)sum;
    }

    private static void Add(int[] arr, int idx, int v)
    {
        if (idx < 0 || idx >= arr.Length) return;
        arr[idx] = arr[idx] + v;
    }

    /// <summary>
    /// Map rAthena's RC_* constants onto our <see cref="BattleRace"/>
    /// enum. RC_All sums into the "All" slot (handled by caller).
    /// </summary>
    private static int ParseRace(string token) => token switch
    {
        "RC_Formless"   => (int)BattleRace.Formless,
        "RC_Undead"     => (int)BattleRace.Undead,
        "RC_Brute"      => (int)BattleRace.Brute,
        "RC_Plant"      => (int)BattleRace.Plant,
        "RC_Insect"     => (int)BattleRace.Insect,
        "RC_Fish"       => (int)BattleRace.Fish,
        "RC_Demon"      => (int)BattleRace.Demon,
        "RC_DemiHuman"  => (int)BattleRace.Demihuman,
        "RC_Angel"      => (int)BattleRace.Angel,
        "RC_Dragon"     => (int)BattleRace.Dragon,
        "RC_Player_Human" => (int)BattleRace.PlayerHuman,
        "RC_Player_Doram" => (int)BattleRace.PlayerDoram,
        "RC_All"        => (int)BattleRace.All,
        _ => -1,
    };

    private static int ParseElement(string token) => token switch
    {
        "Ele_Neutral" => (int)BattleElement.Neutral,
        "Ele_Water"   => (int)BattleElement.Water,
        "Ele_Earth"   => (int)BattleElement.Earth,
        "Ele_Fire"    => (int)BattleElement.Fire,
        "Ele_Wind"    => (int)BattleElement.Wind,
        "Ele_Poison"  => (int)BattleElement.Poison,
        "Ele_Holy"    => (int)BattleElement.Holy,
        "Ele_Dark"    => (int)BattleElement.Dark,
        "Ele_Ghost"   => (int)BattleElement.Ghost,
        "Ele_Undead"  => (int)BattleElement.Undead,
        "Ele_All"     => (int)BattleElement.All,
        _ => -1,
    };

    private static int ParseSize(string token) => token switch
    {
        "Size_Small"  => (int)BattleSize.Small,
        "Size_Medium" => (int)BattleSize.Medium,
        "Size_Large"  => (int)BattleSize.Large,
        "Size_All"    => (int)BattleSize.All,
        _ => -1,
    };

    private static int ParseClass(string token) => token switch
    {
        "Class_Normal"   => (int)BattleClassFlag.Normal,
        "Class_Boss"     => (int)BattleClassFlag.Boss,
        "Class_Guardian" => (int)BattleClassFlag.Guardian,
        "Class_All"      => (int)BattleClassFlag.All,
        _ => -1,
    };
}
