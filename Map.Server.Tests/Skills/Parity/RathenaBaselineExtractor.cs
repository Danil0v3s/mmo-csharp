using System.Text.RegularExpressions;

namespace Map.Server.Tests.Skills.Parity;

/// <summary>
/// T3.1 — static extractor that reads each rAthena C++ skill file
/// (<c>rathena-fork/src/map/skills/{family}/{name}.cpp</c>) and
/// produces a reference call-sequence in the same shape as
/// <see cref="SkillTrace"/>. This lets the parity sweep diff the
/// C# port's recorded behavior against the canonical C++ intent
/// without requiring a live rAthena server, MySQL, or client.
///
/// <para>What we extract:
/// <list type="bullet">
///   <item><c>clif_skill_nodamage(src, target, id, healOrLv)</c> → <c>nodamage</c></item>
///   <item><c>clif_skill_damage(...)</c> → <c>damage-broadcast</c></item>
///   <item><c>clif_skill_fail(*sd, ...)</c> → <c>fail</c></item>
///   <item><c>sc_start[2|4](src,target,SC_X,...)</c> → <c>sc-start</c>, type=X</item>
///   <item><c>status_change_end(target, SC_X)</c> → <c>sc-end</c>, type=X</item>
///   <item><c>skill_attack(BF_X, src, ..., target, ...)</c> → <c>skill-attack</c>, attackType=X</item>
///   <item><c>skill_unitsetting(src, id, lv, x, y, ...)</c> → <c>unit-place</c></item>
///   <item><c>(clif|unit)_blown(...)</c> → <c>blow</c></item>
///   <item><c>status_heal(target, ...)</c> → <c>heal</c></item>
///   <item><c>status_zap(target, hp, sp)</c> → <c>zap</c></item>
///   <item><c>WeaponSkillImpl::castendDamageId(...)</c> → <c>weapon-base-hit</c></item>
/// </list>
/// </para>
///
/// <para>What we don't extract (acceptable divergence):
/// <list type="bullet">
///   <item>Formula values (damage numbers, durations) — handled by
///         the C# recorder's data fields, which the structural diff
///         tolerates.</item>
///   <item>Conditional branches taken — we union all reachable calls.
///         If the C# port emits a subset of the rAthena union, that's
///         a parity match (some branches gated by RNG / state we don't
///         set up).</item>
/// </list>
/// </para>
/// </summary>
public static class RathenaBaselineExtractor
{
    /// <summary>Root path to the rathena-fork source tree.</summary>
    public static string RathenaRoot { get; set; } = "/Volumes/1TB/Projetos/rathena-fork";

    /// <summary>
    /// Parse the .cpp file for <paramref name="family"/>/<paramref name="snake"/>
    /// and return the set of canonical call kinds the file emits.
    /// Returns an empty set if the file is missing (no parity baseline
    /// available — the test then falls back to self-baseline mode).
    /// </summary>
    public static HashSet<string> ExtractCallKinds(string family, string snake)
    {
        var familyDir = family.ToLowerInvariant() switch
        {
            "novice" => "novice",
            "acolyte" => "acolyte",
            "mage" => "mage",
            "archer" => "archer",
            "swordman" => "swordman",
            "merchant" => "merchant",
            "thief" => "thief",
            "ninja" => "ninja",
            "taekwon" => "taekwon",
            "gunslinger" => "gunslinger",
            "homunculus" => "homunculus",
            "summoner" => "summoner",
            "other" => "other",
            "mercenarynpc" => "mercenary",
            "elementalnpc" => "elemental",
            "npc" => "npc",
            _ => family.ToLowerInvariant(),
        };
        var path = Path.Combine(RathenaRoot, "src", "map", "skills", familyDir, snake + ".cpp");
        var hpath = Path.Combine(RathenaRoot, "src", "map", "skills", familyDir, snake + ".hpp");
        if (!File.Exists(path)) return new HashSet<string>();
        var cpp = File.ReadAllText(path);
        var hpp = File.Exists(hpath) ? File.ReadAllText(hpath) : "";
        return ExtractFromSource(cpp + "\n" + hpp);
    }

    /// <summary>Lower-level: parse arbitrary C++ source text.</summary>
    public static HashSet<string> ExtractFromSource(string cpp)
    {
        var kinds = new HashSet<string>();

        // Strip line comments + block comments to reduce false positives.
        cpp = Regex.Replace(cpp, "//.*", "");
        cpp = Regex.Replace(cpp, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // --- direct call detections ---
        if (Regex.IsMatch(cpp, @"\bclif_skill_nodamage\s*\(")) kinds.Add("nodamage");
        // clif_skill_poseffect renders the cast effect like nodamage —
        // structurally equivalent for our purposes.
        if (Regex.IsMatch(cpp, @"\bclif_skill_poseffect\s*\(")) kinds.Add("nodamage");
        if (Regex.IsMatch(cpp, @"\bclif_skill_damage\s*\(")) kinds.Add("damage-broadcast");
        if (Regex.IsMatch(cpp, @"\bclif_skill_fail\s*\(")) kinds.Add("fail");
        if (Regex.IsMatch(cpp, @"\bsc_start[24]?\s*\(")
            || Regex.IsMatch(cpp, @"\bstatus_change_start\s*\("))
            kinds.Add("sc-start");
        if (Regex.IsMatch(cpp, @"\bstatus_change_end\s*\(")) kinds.Add("sc-end");
        if (Regex.IsMatch(cpp, @"\bskill_attack\s*\(")) kinds.Add("skill-attack");
        if (Regex.IsMatch(cpp, @"\bskill_unitsetting\s*\(")) kinds.Add("unit-place");
        if (Regex.IsMatch(cpp, @"\b(clif_blown|unit_blown_by|skill_blown|skill_blown_by|skill_castend_blown)\s*\("))
            kinds.Add("blow");
        // rAthena heal family: status_heal, status_percent_heal,
        // status_fixed_revive (revive also does HP+SP delta via heal pipe).
        if (Regex.IsMatch(cpp, @"\bstatus_heal\s*\(")
            || Regex.IsMatch(cpp, @"\bstatus_percent_heal\s*\(")
            || Regex.IsMatch(cpp, @"\bstatus_fixed_revive\s*\("))
            kinds.Add("heal");
        // Zap family: status_zap, status_percent_damage (without can_kill
        // = SP loss too).
        if (Regex.IsMatch(cpp, @"\bstatus_zap\s*\(")
            || Regex.IsMatch(cpp, @"\bstatus_percent_damage\s*\("))
            kinds.Add("zap");
        if (Regex.IsMatch(cpp, @"\bunit_movepos\s*\(")) kinds.Add("move-pos");
        if (Regex.IsMatch(cpp, @"\bskill_check_unit_movepos\s*\(")) kinds.Add("move-pos");

        // --- base-class call inference ---
        // WeaponSkillImpl::castendDamageId → emits both nodamage and
        // damage-broadcast through the standard weapon-hit pipeline.
        if (Regex.IsMatch(cpp, @"WeaponSkillImpl::castendDamageId"))
        {
            kinds.Add("damage-broadcast");
            kinds.Add("nodamage");
            kinds.Add("skill-attack");
        }
        if (Regex.IsMatch(cpp, @"RecursiveDamageSplashSkillImpl::"))
        {
            kinds.Add("damage-broadcast");
            kinds.Add("skill-attack");
        }
        // A class that inherits StatusSkillImpl (visible in header or
        // constructor delegation) emits nodamage + sc-start through
        // the base, regardless of whether the subclass adds its own
        // castendNoDamageId override.
        if (Regex.IsMatch(cpp, @":\s*public\s+StatusSkillImpl\b")
            || Regex.IsMatch(cpp, @":\s*StatusSkillImpl\s*\("))
        {
            kinds.Add("nodamage");
            kinds.Add("sc-start");
        }
        // WeaponSkillImpl-derived classes always do damage broadcast.
        if (Regex.IsMatch(cpp, @":\s*public\s+WeaponSkillImpl\b")
            || Regex.IsMatch(cpp, @":\s*WeaponSkillImpl\s*\("))
        {
            kinds.Add("nodamage");
            kinds.Add("damage-broadcast");
            kinds.Add("skill-attack");
        }
        if (Regex.IsMatch(cpp, @":\s*public\s+RecursiveDamageSplashSkillImpl\b")
            || Regex.IsMatch(cpp, @":\s*RecursiveDamageSplashSkillImpl\s*\("))
        {
            kinds.Add("nodamage");
            kinds.Add("damage-broadcast");
            kinds.Add("skill-attack");
        }

        // --- wildcard relaxation ---
        // Constructs that defer or dispatch to another skill handler
        // can produce any downstream kind. When present, widen the
        // allow-set so static analysis doesn't false-positive.
        var isWildcard =
            Regex.IsMatch(cpp, @"\bskill_addtimerskill\s*\(")
            || Regex.IsMatch(cpp, @"\bmap_foreach\w*\s*\([^)]*skill_castend_")
            || Regex.IsMatch(cpp, @"\bskill_castend_(damage|nodamage)_id\b")
            || Regex.IsMatch(cpp, @"\bskill_addunit\s*\(")
            || Regex.IsMatch(cpp, @"\bopt1\b")            // direct opt1 mutation = SC equiv
            || Regex.IsMatch(cpp, @"\bclif_changeoption\b");
        if (isWildcard)
        {
            kinds.Add("skill-attack");
            kinds.Add("sc-start");
            kinds.Add("sc-end");
            kinds.Add("damage-broadcast");
            kinds.Add("nodamage");
            kinds.Add("damage");
            kinds.Add("unit-place");
        }

        return kinds;
    }

    /// <summary>
    /// Map a C# class name in <c>Map.Server.Skills.Behaviors.{Family}</c>
    /// to its rAthena snake_case file stem. Most ports follow the
    /// rAthena file naming directly (e.g. <c>JackFrostNova</c> →
    /// <c>jackfrostnova</c>); some need explicit overrides because the
    /// C# rename diverged.
    /// </summary>
    public static string ClassNameToSnake(string className)
    {
        // PascalCase → lowercase, no separators (matches rAthena's
        // file naming convention).
        return string.Concat(className.Select(c => char.ToLowerInvariant(c)));
    }
}
