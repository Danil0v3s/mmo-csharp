using System;
using System.Collections.Generic;
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

    // COMBAT-23 — the 1-arg flag form: `bonus bNoCastCancel;` (no value).
    private static readonly Regex BonusFlag = new(
        @"bonus\s+b(?<key>[A-Za-z]+)\s*;",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // COMBAT-43 — the constant-arg form `bonus bIgnoreDefRace,RC_X;` /
    // `bonus bIgnoreDefClass,Class_X;` — the second arg is a race/class CONSTANT,
    // not a number, so BonusFlat (which requires digits) never matches it.
    private static readonly Regex BonusIgnoreDef = new(
        @"bonus\s+b(?<key>IgnoreDefRace|IgnoreDefClass)\s*,\s*(?<idx>\w+)\s*;",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Bonus2Indexed = new(
        // The index token may be a constant (RC_X / Ele_X / a skill name) or a
        // quoted skill name (bonus2 bSkillAtk,"MG_FIREBOLT",20). COMBAT-22 widened
        // this to accept the optional surrounding quotes.
        @"bonus2\s+b(?<key>[A-Za-z]+)\s*,\s*""?(?<idx>[A-Za-z_0-9]+)""?\s*,\s*(?<v>-?\d+)\s*;",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // COMBAT-22 — skill-name → id map, reflected once from the SkillIds
    // constants. bonus2 bSkillAtk / bVariableCastrate index by the rAthena
    // skill constant name (e.g. MG_FIREBOLT); raw numeric ids are also accepted.
    private static readonly Dictionary<string, ushort> SkillNameToId = BuildSkillNameMap();

    private static Dictionary<string, ushort> BuildSkillNameMap()
    {
        var map = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in typeof(Map.Server.Skills.SkillIds).GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (f.IsLiteral && f.FieldType == typeof(ushort) && f.GetRawConstantValue() is ushort id)
                map[f.Name] = id;
        }
        return map;
    }

    /// <summary>Resolve a skill index token (constant name or raw id) → skill id.</summary>
    private static bool TryResolveSkill(string token, out ushort skillId)
    {
        if (ushort.TryParse(token, out skillId)) return true;
        return SkillNameToId.TryGetValue(token, out skillId);
    }

    private static void AddSkillMap(Dictionary<ushort, int> map, string idxToken, int v)
    {
        if (!TryResolveSkill(idxToken, out var sid)) return; // unknown skill → skip (bias to miss)
        map[sid] = map.GetValueOrDefault(sid) + v;
    }

    public static void Apply(string? script, EquipBonusBundle bundle)
    {
        if (string.IsNullOrWhiteSpace(script)) return;

        foreach (Match m in BonusFlat.Matches(script))
        {
            var key = m.Groups["key"].Value;
            if (!int.TryParse(m.Groups["v"].Value, out var v)) continue;
            ApplyFlat(bundle, key, v);
        }

        // COMBAT-23 — flag-form bonuses (no value). Match AFTER the valued form
        // so `bonus bAtk,10;` isn't mis-read; the regex requires a `;` right
        // after the key, so a valued bonus (which has a comma) never matches.
        foreach (Match m in BonusFlag.Matches(script))
            ApplyFlag(bundle, m.Groups["key"].Value);

        foreach (Match m in Bonus2Indexed.Matches(script))
        {
            var key = m.Groups["key"].Value;
            var idxToken = m.Groups["idx"].Value;
            if (!int.TryParse(m.Groups["v"].Value, out var v)) continue;
            ApplyIndexed(bundle, key, idxToken, v);
        }

        // COMBAT-43 — ignore-def (constant-arg form). The DEF-reduction stage reads
        // these bitmasks (rAthena SP_IGNORE_DEF_RACE / SP_IGNORE_DEF_CLASS).
        foreach (Match m in BonusIgnoreDef.Matches(script))
            ApplyIgnoreDef(bundle, m.Groups["key"].Value, m.Groups["idx"].Value);
    }

    /// <summary>
    /// COMBAT-43 — OR the race/class bit for <c>bonus bIgnoreDefRace,RC_X</c> /
    /// <c>bonus bIgnoreDefClass,Class_X</c>. <c>RC_All</c>/<c>Class_All</c> set every
    /// real race/class bit (rAthena's "all" sentinel).
    /// </summary>
    internal static void ApplyIgnoreDef(EquipBonusBundle b, string key, string idxToken)
    {
        if (string.Equals(key, "IgnoreDefRace", StringComparison.OrdinalIgnoreCase))
        {
            var r = ParseRace(idxToken);
            if (r == (int)BattleRace.All) b.IgnoreDefRace |= (1 << (int)BattleRace.PlayerDoram + 1) - 1; // all real races
            else if (r >= 0) b.IgnoreDefRace |= 1 << r;
        }
        else // IgnoreDefClass
        {
            var c = ParseClass(idxToken);
            if (c == (int)BattleClassFlag.All) b.IgnoreDefClass |= (1 << (int)BattleClassFlag.Guardian + 1) - 1; // all classes
            else if (c >= 0) b.IgnoreDefClass |= 1 << c;
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
            // COMBAT-07 — flat ms cast adds (positive = faster; rAthena
            // skill_vfcastfix subtracts add_varcast/add_fixcast).
            case "variablecast":   b.AddVarCastMs += v; break;
            case "fixedcast":      b.AddFixCastMs += v; break;
            // COMBAT-06 — damage / defense rate bonuses.
            case "atkrate":        b.AtkRate += v; break;
            case "matkrate":       b.MatkRate += v; break;
            case "def":            b.FlatDef += v; break;
            case "mdef":           b.FlatMdef += v; break;
            case "defrate":        b.DefRate += v; break;
            case "mdefrate":       b.MdefRate += v; break;
            // COMBAT-17 — bonus bDoubleRate, n (SP_DOUBLE_RATE). rAthena
            // keeps the MAX across sources (pc.cpp:3925), not a sum.
            case "doublerate":     if (v > b.DoubleRate) b.DoubleRate = v; break;
            // COMBAT-23 — single-value pc_bonus tail.
            case "healpower":      b.HealPower += v; break;
            case "healpower2":     b.HealPower2 += v; break;
            case "hprecovrate":    b.HpRecovRate += v; break;
            case "sprecovrate":    b.SpRecovRate += v; break;
            // SP_SPEED_RATE: non-stackable, rAthena keeps the MIN of -val.
            case "speedrate":      { var sr = -v; if (sr < b.SpeedRate) b.SpeedRate = sr; break; }
            case "speedaddrate":   b.SpeedAddRate += v; break;
            case "criticalrate":   b.CriticalRate += v; break;
            case "usesprate":      b.UseSpRate += v; break;
            case "addmaxweight":   b.AddMaxWeight += v; break;
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

    /// <summary>
    /// COMBAT-23 — apply a 1-arg flag-form bonus (<c>bonus bX;</c>) to the
    /// bundle. Also reachable from <see cref="ScriptedBonusHost"/> so the V8
    /// host routes flags through the same switch.
    /// </summary>
    internal static void ApplyFlagBonus(EquipBonusBundle b, string key) => ApplyFlag(b, key);

    private static void ApplyFlag(EquipBonusBundle b, string key)
    {
        switch (key.ToLowerInvariant())
        {
            // COMBAT-08/27 own the consumer (cast-interrupt gate); this is the parse.
            // bNoCastCancel is GvG/BG-gated; bNoCastCancel2 is unconditional.
            case "nocastcancel":    b.NoCastCancel = true; break;
            case "nocastcancel2":   b.NoCastCancel2 = true; break;
            case "unbreakablearmor":   b.UnbreakableArmor = true; break;
            case "unbreakableweapon":  b.UnbreakableWeapon = true; break;
            case "unbreakablehelm":    b.UnbreakableHelm = true; break;
            case "unbreakableshield":  b.UnbreakableShield = true; break;
            case "unbreakableshoes":   b.UnbreakableShoes = true; break;
            case "unbreakablegarment": b.UnbreakableGarment = true; break;
            case "intravision":        b.Intravision = true; break;
            // Other 1-arg flags (bNoWalkDelay / bNoGemStone / bPerfectHide …)
            // land with their consumers in COMBAT-45.
        }
    }

    private static void ApplyIndexed(EquipBonusBundle b, string key, string idxToken, int v)
    {
        switch (key.ToLowerInvariant())
        {
            case "addrace": Add(b.AddRace, ParseRace(idxToken), v); break;
            case "subrace": Add(b.SubRace, ParseRace(idxToken), v); break;
            // COMBAT-77 — bonus2 bIgnoreResRace, r, n (SP_IGNORE_RES_RACE): n% of the target's
            // physical Res ignored vs race r. RC_All → the All slot (summed by the combat reader).
            case "ignoreresrace": Add(b.IgnoreResRace, ParseRace(idxToken), v); break;
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
            // COMBAT-21 — advanced cardfix: offensive magic-per-race and the
            // per-race critical-rate bonus (rAthena stores cri ×10, so ×10 here).
            case "magicaddrace":    Add(b.MagicAddRace, ParseRace(idxToken), v); break;
            case "criticaladdrace": Add(b.CritAddRace,  ParseRace(idxToken), v * 10); break;
            // COMBAT-63 — distinct offensive magic ele/size/class arrays (rAthena keeps
            // these separate from the weapon addele/addsize/addclass).
            case "magicaddele":     Add(b.MagicAddEle,   ParseElement(idxToken), v); break;
            case "magicaddsize":    Add(b.MagicAddSize,  ParseSize(idxToken),    v); break;
            case "magicaddclass":   Add(b.MagicAddClass, ParseClass(idxToken),   v); break;
            // COMBAT-22 — per-skill bonus2 maps (index is a skill name / id).
            case "skillatk":         AddSkillMap(b.SkillAtk, idxToken, v); break;
            // COMBAT-64 — defender per-skill incoming-damage reduction (bonus2 bSubSkill, sk, n).
            case "subskill":         AddSkillMap(b.SubSkillAtk, idxToken, v); break;
            // COMBAT-44 — per-skill heal-output % (bonus2 bSkillHeal, sk, n).
            case "skillheal":        AddSkillMap(b.SkillHeal, idxToken, v); break;
            // COMBAT-44 — on-hit vanish: bonus2 bHPVanishRate,rate,per — the index
            // token IS the rate (a number), the value is the per. Last-write wins
            // (one vanish bonus per item is the common case).
            case "hpvanishrate":
                if (int.TryParse(idxToken, out var hpr)) { b.HpVanishRate = hpr; b.HpVanishPer = v; }
                break;
            case "spvanishrate":
                if (int.TryParse(idxToken, out var spr)) { b.SpVanishRate = spr; b.SpVanishPer = v; }
                break;
            // rAthena stores the cast-rate per-skill INVERSED (-val); the
            // cast-timing consumer (COMBAT-24) reads these as a reduction.
            case "variablecastrate": AddSkillMap(b.SkillVarCastrate, idxToken, -v); break;
            case "fixedcastrate":    AddSkillMap(b.SkillFixCastrate, idxToken, -v); break;
            // COMBAT-24 — per-skill FLAT cast-ms add (rAthena adds the raw value;
            // negative = faster). bonus2 bSkillVariableCast / bSkillFixedCast.
            case "skillvariablecast": AddSkillMap(b.SkillVarCast, idxToken, v); break;
            case "skillfixedcast":    AddSkillMap(b.SkillFixCast, idxToken, v); break;
            // bAddRace2, bIgnoreDefRate, bSubDefEle, ... — race2 classification
            // + flag-matched lists land in COMBAT-43.
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
