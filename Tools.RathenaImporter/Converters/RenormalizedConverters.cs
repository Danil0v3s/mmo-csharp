using System.Text;
using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace Tools.RathenaImporter.Converters;

// ============================================================================
// DB-8z-SG — re-normalized YAML→SQL converters.
//
// Replaces the lossy PayloadJson converters with typed-column emitters
// matching the DB-8a..DB-8i entity surface. Each converter walks the
// rAthena YAML and emits REPLACE INTO statements against the proper
// typed parent + child tables.
//
// One file, many classes — same convention as FlatConverters.cs /
// PayloadConverters.cs. Bundled by wave so reviewers can diff against
// the corresponding DB-8 entity definitions.
//
// Pattern per wave:
//   1. Walk Body sequence.
//   2. For each row, emit parent REPLACE.
//   3. For each nested array/map, emit child REPLACE rows with the
//      composite key components derived from parent + position.
// ============================================================================

// ============================================================================
// DB-8a: tier-1 flats (level_penalty, attr_fix, reputation_group)
// ============================================================================

/// <summary><c>db/re/level_penalty.yml</c> → level_penalty_db + level_penalty_difference_db.</summary>
public sealed class LevelPenaltyRenormConverter : IYamlToSqlConverter
{
    public string Name => "level_penalty";
    public string SourceYamlPath => "db/re/level_penalty.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_level_penalty.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var type = row.Str("Type");
            if (string.IsNullOrEmpty(type)) continue;
            sb.AppendLine(SqlEmit.Replace("level_penalty_db",
                new[] { "penalty_type" }, new object?[] { type }));
            n++;
            foreach (var diff in row.Rows("LevelDifferences"))
            {
                var d = diff.Int("Difference");
                var rate = diff.Int("Rate");
                if (d == null) continue;
                sb.AppendLine(SqlEmit.Replace("level_penalty_difference_db",
                    new[] { "penalty_type", "difference", "rate" },
                    new object?[] { type, d.Value, rate ?? 100 }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/attr_fix.yml</c> → attr_fix_db. Per-Level row carries a 10×10
/// element matrix (AttackerElement → DefenderElement → Multiplier);
/// we flatten to one row per (Level, AttackerElement, DefenderElement).
/// </summary>
public sealed class AttrFixRenormConverter : IYamlToSqlConverter
{
    public string Name => "attr_fix";
    public string SourceYamlPath => "db/re/attr_fix.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_attr_fix.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lvl = row.Int("Level");
            if (lvl == null) continue;
            foreach (var kv in row.Children)
            {
                var atkElem = (kv.Key as YamlScalarNode)?.Value;
                if (atkElem == null || atkElem == "Level") continue;
                if (kv.Value is not YamlMappingNode defMap) continue;
                foreach (var defKv in defMap.Children)
                {
                    var defElem = (defKv.Key as YamlScalarNode)?.Value;
                    var mult = (defKv.Value as YamlScalarNode)?.Value;
                    if (defElem == null || mult == null) continue;
                    if (!int.TryParse(mult, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m)) continue;
                    sb.AppendLine(SqlEmit.Replace("attr_fix_db",
                        new[] { "level", "attacker_element", "defender_element", "multiplier" },
                        new object?[] { lvl.Value, atkElem, defElem, m }));
                    n++;
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/reputation_group.yml</c> → reputation_group_db + reputation_group_member_db.</summary>
public sealed class ReputationGroupRenormConverter : IYamlToSqlConverter
{
    public string Name => "reputation_group";
    public string SourceYamlPath => "db/re/reputation_group.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_reputation_group.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("reputation_group_db",
                new[] { "id", "script_name", "name" },
                new object?[] { id.Value, row.Str("ScriptName") ?? "", row.Str("Name") ?? "" }));
            n++;
            // Reputations: list of { Id: <int> } entries.
            foreach (var member in row.Rows("Reputations"))
            {
                var rep = member.Int("Id");
                if (rep == null) continue;
                sb.AppendLine(SqlEmit.Replace("reputation_group_member_db",
                    new[] { "group_id", "reputation_id" },
                    new object?[] { id.Value, rep.Value }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8b: tier-2 single-child catalogs
// ============================================================================

/// <summary><c>db/re/mob_summon.yml</c> → mob_summon_db + mob_summon_entry_db.</summary>
public sealed class MobSummonRenormConverter : IYamlToSqlConverter
{
    public string Name => "mob_summon";
    public string SourceYamlPath => "db/re/mob_summon.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mob_summon.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var group = row.Str("Group");
            if (string.IsNullOrEmpty(group)) continue;
            sb.AppendLine(SqlEmit.Replace("mob_summon_db",
                new[] { "group_name", "default_mob_aegis" },
                new object?[] { group, row.Str("Default") ?? "" }));
            n++;
            foreach (var s in row.Rows("Summon"))
            {
                var mob = s.Str("Mob");
                var rate = s.Int("Rate") ?? 1;
                if (string.IsNullOrEmpty(mob)) continue;
                sb.AppendLine(SqlEmit.Replace("mob_summon_entry_db",
                    new[] { "group_name", "mob_aegis", "rate" },
                    new object?[] { group, mob, rate }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/attendance.yml</c> → attendance_catalog_db + attendance_catalog_reward_db.</summary>
public sealed class AttendanceRenormConverter : IYamlToSqlConverter
{
    public string Name => "attendance";
    public string SourceYamlPath => "db/re/attendance.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_attendance_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        var nextId = 1;
        foreach (var row in body.Rows())
        {
            var start = row.Int("Start") ?? row.Int("StartDate") ?? 0;
            var end = row.Int("End") ?? row.Int("EndDate") ?? 0;
            var id = row.Int("Id") ?? nextId++;
            sb.AppendLine(SqlEmit.Replace("attendance_catalog_db",
                new[] { "attendance_id", "start_date", "end_date" },
                new object?[] { id, start, end }));
            n++;
            foreach (var reward in row.Rows("Rewards"))
            {
                var day = reward.Int("Day");
                var item = reward.Int("ItemId") ?? 0;
                var amount = reward.Int("Amount") ?? 1;
                if (day == null) continue;
                sb.AppendLine(SqlEmit.Replace("attendance_catalog_reward_db",
                    new[] { "attendance_id", "day", "item_id", "amount" },
                    new object?[] { id, day.Value, item, amount }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/item_cash.yml</c> → item_cash_db + item_cash_entry_db.</summary>
public sealed class ItemCashRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_cash";
    public string SourceYamlPath => "db/item_cash.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_cash.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var tab = row.Str("Tab");
            if (string.IsNullOrEmpty(tab)) continue;
            sb.AppendLine(SqlEmit.Replace("item_cash_db",
                new[] { "tab" }, new object?[] { tab }));
            n++;
            foreach (var entry in row.Rows("Items"))
            {
                var aegis = entry.Str("Item");
                var price = entry.Int("Price") ?? 0;
                if (string.IsNullOrEmpty(aegis)) continue;
                sb.AppendLine(SqlEmit.Replace("item_cash_entry_db",
                    new[] { "tab", "item_aegis", "price" },
                    new object?[] { tab, aegis, price }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/item_group_db.yml</c> → item_group_catalog_db +
/// item_group_catalog_entry_db (SubGroup flattened into entry row).
/// </summary>
public sealed class ItemGroupCatalogRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_group_db";
    public string SourceYamlPath => "db/re/item_group_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_group_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var group = row.Str("Group");
            if (string.IsNullOrEmpty(group)) continue;
            sb.AppendLine(SqlEmit.Replace("item_group_catalog_db",
                new[] { "group_name" }, new object?[] { group }));
            n++;
            foreach (var sub in row.Rows("SubGroups"))
            {
                var subId = sub.Int("SubGroup") ?? 1;
                var idx = 0;
                foreach (var entry in sub.Rows("List"))
                {
                    var entryIdx = entry.Int("Index") ?? idx;
                    var item = entry.Str("Item");
                    if (string.IsNullOrEmpty(item)) { idx++; continue; }
                    sb.AppendLine(SqlEmit.Replace("item_group_catalog_entry_db",
                        new[] { "group_name", "sub_group", "entry_index", "item_aegis", "rate",
                                "announced", "amount", "duration_hours", "refine", "random_option_group" },
                        new object?[] {
                            group, subId, entryIdx, item, entry.Int("Rate") ?? 1,
                            entry.Bool("Announced") ?? false, entry.Int("Amount") ?? 1,
                            entry.Int("Duration"), entry.Int("Refine"), entry.Str("RandomOptionGroup")
                        }));
                    idx++;
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/item_packages.yml</c> → item_package_db + item_package_entry_db.</summary>
public sealed class ItemPackageRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_packages";
    public string SourceYamlPath => "db/re/item_packages.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_packages.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var opener = row.Str("Item");
            if (string.IsNullOrEmpty(opener)) continue;
            sb.AppendLine(SqlEmit.Replace("item_package_db",
                new[] { "item_aegis" }, new object?[] { opener }));
            n++;
            foreach (var grp in row.Rows("Groups"))
            {
                var groupId = grp.Int("Group") ?? 0;
                foreach (var item in grp.Rows("Items"))
                {
                    var contained = item.Str("Item");
                    if (string.IsNullOrEmpty(contained)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_package_entry_db",
                        new[] { "item_aegis", "group_id", "contained_item_aegis",
                                "amount", "refine", "rental_hours", "random_option_group" },
                        new object?[] {
                            opener, groupId, contained,
                            item.Int("Amount") ?? 1, item.Int("Refine"),
                            item.Int("RentalHours"), item.Str("RandomOptionGroup")
                        }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/item_combos.yml</c> → item_combo_db + item_combo_member_db.
/// rAthena groups multiple Combo arrays under one shared Script; we
/// denormalize per-Combo so each emit gets its own ComboId + script copy.
/// </summary>
public sealed class ItemComboRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_combos";
    public string SourceYamlPath => "db/re/item_combos.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_combos.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        var comboId = 1;
        foreach (var row in body.Rows())
        {
            var script = row.Str("Script") ?? "";
            if (row.Get("Combos") is not YamlSequenceNode combos) continue;
            foreach (var combo in combos.Children.OfType<YamlMappingNode>())
            {
                if (combo.Get("Combo") is not YamlSequenceNode members) continue;
                sb.AppendLine(SqlEmit.Replace("item_combo_db",
                    new[] { "combo_id", "script" },
                    new object?[] { comboId, script }));
                n++;
                foreach (var m in members.Children.OfType<YamlScalarNode>())
                {
                    if (string.IsNullOrEmpty(m.Value)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_combo_member_db",
                        new[] { "combo_id", "member_item_aegis" },
                        new object?[] { comboId, m.Value }));
                }
                comboId++;
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8c: skill trees
// ============================================================================

/// <summary>
/// <c>db/re/skill_tree.yml</c> → skill_tree_db + skill_tree_inherit_db +
/// skill_tree_entry_db + skill_tree_requirement_db.
/// </summary>
public sealed class SkillTreeRenormConverter : IYamlToSqlConverter
{
    public string Name => "skill_tree";
    public string SourceYamlPath => "db/re/skill_tree.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_skill_tree.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var job = row.Str("Job");
            if (string.IsNullOrEmpty(job)) continue;
            sb.AppendLine(SqlEmit.Replace("skill_tree_db",
                new[] { "job_aegis" }, new object?[] { job }));
            n++;
            // Inherit: { Novice: true, Swordman: true } → one row per parent.
            if (row.Get("Inherit") is YamlMappingNode inheritMap)
            {
                foreach (var kv in inheritMap.Children)
                {
                    var parent = (kv.Key as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(parent)) continue;
                    sb.AppendLine(SqlEmit.Replace("skill_tree_inherit_db",
                        new[] { "child_job_aegis", "parent_job_aegis" },
                        new object?[] { job, parent }));
                }
            }
            foreach (var entry in row.Rows("Tree"))
            {
                var skill = entry.Str("Name");
                if (string.IsNullOrEmpty(skill)) continue;
                sb.AppendLine(SqlEmit.Replace("skill_tree_entry_db",
                    new[] { "job_aegis", "skill_aegis", "max_level", "exclude" },
                    new object?[] { job, skill, entry.Int("MaxLevel") ?? 1, entry.Bool("Exclude") ?? false }));
                foreach (var req in entry.Rows("Requires"))
                {
                    var rSkill = req.Str("Name");
                    var rLvl = req.Int("Level") ?? 1;
                    if (string.IsNullOrEmpty(rSkill)) continue;
                    sb.AppendLine(SqlEmit.Replace("skill_tree_requirement_db",
                        new[] { "job_aegis", "skill_aegis", "required_skill_aegis", "required_level" },
                        new object?[] { job, skill, rSkill, rLvl }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/guild_skill_tree.yml</c> → guild_skill_tree_db + guild_skill_tree_requirement_db.
/// </summary>
public sealed class GuildSkillTreeRenormConverter : IYamlToSqlConverter
{
    public string Name => "guild_skill_tree";
    public string SourceYamlPath => "db/re/guild_skill_tree.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_guild_skill_tree.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var skill = row.Str("Id");
            if (string.IsNullOrEmpty(skill)) continue;
            sb.AppendLine(SqlEmit.Replace("guild_skill_tree_db",
                new[] { "skill_aegis", "max_level" },
                new object?[] { skill, row.Int("MaxLevel") ?? 1 }));
            n++;
            foreach (var req in row.Rows("Required"))
            {
                var rSkill = req.Str("Id");
                if (string.IsNullOrEmpty(rSkill)) continue;
                sb.AppendLine(SqlEmit.Replace("guild_skill_tree_requirement_db",
                    new[] { "skill_aegis", "required_skill_aegis", "required_level" },
                    new object?[] { skill, rSkill, req.Int("Level") ?? 1 }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8d: job tables (denormalize Jobs: map per row)
// ============================================================================

internal static class JobRowHelpers
{
    /// <summary>Enumerate the job Aegis names listed under the row's <c>Jobs:</c> map.</summary>
    public static IEnumerable<string> EnumerateJobs(YamlMappingNode row)
    {
        if (row.Get("Jobs") is not YamlMappingNode jobs) yield break;
        foreach (var kv in jobs.Children)
        {
            var name = (kv.Key as YamlScalarNode)?.Value;
            var enabled = (kv.Value as YamlScalarNode)?.Value;
            if (string.IsNullOrEmpty(name)) continue;
            if (enabled != null && enabled.Equals("false", StringComparison.OrdinalIgnoreCase)) continue;
            yield return name;
        }
    }
}

/// <summary>
/// <c>db/re/job_stats.yml</c> → job_info_db + job_bonus_stats_db.
/// rAthena groups multiple jobs sharing a stat block under one Jobs map;
/// we emit one job_info row + one job_bonus_stats row per (job, level).
/// </summary>
public sealed class JobStatsRenormConverter : IYamlToSqlConverter
{
    public string Name => "job_stats";
    public string SourceYamlPath => "db/re/job_stats.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_stats.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var jobs = JobRowHelpers.EnumerateJobs(row).ToList();
            if (jobs.Count == 0) continue;
            var maxWeight = row.Int("MaxWeight") ?? 20000;
            var hpFactor = row.Int("HpFactor") ?? 0;
            var hpIncrease = row.Int("HpIncrease") ?? 500;
            var spFactor = row.Int("SpFactor") ?? 0;
            var spIncrease = row.Int("SpIncrease") ?? 100;
            foreach (var job in jobs)
            {
                sb.AppendLine(SqlEmit.Replace("job_info_db",
                    new[] { "job_aegis", "max_weight", "hp_factor", "hp_increase", "sp_factor", "sp_increase" },
                    new object?[] { job, maxWeight, hpFactor, hpIncrease, spFactor, spIncrease }));
                n++;
                foreach (var bonus in row.Rows("BonusStats"))
                {
                    var lvl = bonus.Int("Level");
                    if (lvl == null) continue;
                    sb.AppendLine(SqlEmit.Replace("job_bonus_stats_db",
                        new[] { "job_aegis", "level", "str", "agi", "vit", "int_stat", "dex", "luk",
                                "pow", "sta", "wis", "spl", "con", "crt" },
                        new object?[] {
                            job, lvl.Value,
                            bonus.Int("Str") ?? 0, bonus.Int("Agi") ?? 0, bonus.Int("Vit") ?? 0,
                            bonus.Int("Int") ?? 0, bonus.Int("Dex") ?? 0, bonus.Int("Luk") ?? 0,
                            bonus.Int("Pow") ?? 0, bonus.Int("Sta") ?? 0, bonus.Int("Wis") ?? 0,
                            bonus.Int("Spl") ?? 0, bonus.Int("Con") ?? 0, bonus.Int("Crt") ?? 0
                        }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/job_exp.yml</c> → job_exp_db + job_max_level_db.
/// Each Body row has Jobs map + MaxBaseLevel/MaxJobLevel + Exp[] (BaseExp/JobExp per level).
/// </summary>
public sealed class JobExpRenormConverter : IYamlToSqlConverter
{
    public string Name => "job_exp";
    public string SourceYamlPath => "db/re/job_exp.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_exp.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var jobs = JobRowHelpers.EnumerateJobs(row).ToList();
            if (jobs.Count == 0) continue;
            var maxBase = row.Int("MaxBaseLevel");
            var maxJob = row.Int("MaxJobLevel");
            // rAthena yml uses Exp: [{Level: 1, Exp: <n>}, …]; "Exp" is BaseExp if MaxBaseLevel set,
            // JobExp if MaxJobLevel set. The row carries one of the two — split into the right column.
            var hasBaseLevel = maxBase != null;
            foreach (var job in jobs)
            {
                sb.AppendLine(SqlEmit.Replace("job_max_level_db",
                    new[] { "job_aegis", "max_base_level", "max_job_level" },
                    new object?[] { job, maxBase, maxJob }));
                n++;
                foreach (var e in row.Rows("Exp"))
                {
                    var lvl = e.Int("Level");
                    var exp = e.Long("Exp");
                    if (lvl == null || exp == null) continue;
                    sb.AppendLine(SqlEmit.Replace("job_exp_db",
                        new[] { "job_aegis", "level", "base_exp", "job_exp" },
                        new object?[] {
                            job, lvl.Value,
                            hasBaseLevel ? (object)exp.Value : null,
                            hasBaseLevel ? null : (object?)exp.Value
                        }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/job_basepoints.yml</c> → job_base_points_db.
/// Each Body row has Jobs map + BaseHp[] + BaseSp[] (+ BaseAp[] for 4th-class).
/// </summary>
public sealed class JobBasePointsRenormConverter : IYamlToSqlConverter
{
    public string Name => "job_basepoints";
    public string SourceYamlPath => "db/re/job_basepoints.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_basepoints.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var jobs = JobRowHelpers.EnumerateJobs(row).ToList();
            if (jobs.Count == 0) continue;
            // Merge BaseHp/BaseSp/BaseAp into per-level map.
            var perLevel = new Dictionary<int, (int? hp, int? sp, int? ap)>();
            foreach (var hp in row.Rows("BaseHp"))
            {
                var l = hp.Int("Level"); var v = hp.Int("Hp");
                if (l == null || v == null) continue;
                var cur = perLevel.TryGetValue(l.Value, out var c) ? c : (null, null, null);
                perLevel[l.Value] = (v, cur.sp, cur.ap);
            }
            foreach (var sp in row.Rows("BaseSp"))
            {
                var l = sp.Int("Level"); var v = sp.Int("Sp");
                if (l == null || v == null) continue;
                var cur = perLevel.TryGetValue(l.Value, out var c) ? c : (null, null, null);
                perLevel[l.Value] = (cur.hp, v, cur.ap);
            }
            foreach (var ap in row.Rows("BaseAp"))
            {
                var l = ap.Int("Level"); var v = ap.Int("Ap");
                if (l == null || v == null) continue;
                var cur = perLevel.TryGetValue(l.Value, out var c) ? c : (null, null, null);
                perLevel[l.Value] = (cur.hp, cur.sp, v);
            }
            foreach (var job in jobs)
            {
                foreach (var (lvl, vals) in perLevel.OrderBy(kv => kv.Key))
                {
                    sb.AppendLine(SqlEmit.Replace("job_base_points_db",
                        new[] { "job_aegis", "level", "hp", "sp", "ap" },
                        new object?[] { job, lvl, vals.hp ?? 0, vals.sp ?? 0, vals.ap }));
                    n++;
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8e: status.yml (parent + flag with category discriminator)
// ============================================================================

/// <summary>
/// <c>db/re/status.yml</c> → status_db + status_db_flag (flat). The 7+
/// nested boolean maps in the YAML (States / CalcFlags / Flags / Fail /
/// EndOnStart / EndOnEnd / EndOnRestart / EndReturn) collapse into
/// one child table with a category discriminator column.
/// </summary>
public sealed class StatusYmlRenormConverter : IYamlToSqlConverter
{
    public string Name => "status_yml";
    public string SourceYamlPath => "db/re/status.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_status_yml.sql";

    private static readonly (string yamlKey, string category)[] FlagMaps =
    {
        ("States",      "State"),
        ("CalcFlags",   "CalcFlag"),
        ("Flags",       "Flag"),
        ("Fail",        "Fail"),
        ("EndOnStart",  "EndOnStart"),
        ("EndReturn",   "EndReturn"),
        ("EndOnEnd",    "EndOnEnd"),
        ("EndOnRestart","EndOnRestart"),
    };

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var name = row.Str("Status");
            if (string.IsNullOrEmpty(name)) continue;
            sb.AppendLine(SqlEmit.Replace("status_db",
                new[] { "status_name", "duration_lookup", "opt1", "opt2", "opt3" },
                new object?[] {
                    name,
                    row.Str("DurationLookup"),
                    row.Str("Opt1"),
                    row.Str("Opt2"),
                    row.Str("Opt3"),
                }));
            n++;
            foreach (var (yamlKey, category) in FlagMaps)
            {
                if (row.Get(yamlKey) is not YamlMappingNode flagMap) continue;
                foreach (var kv in flagMap.Children)
                {
                    var flagName = (kv.Key as YamlScalarNode)?.Value;
                    var enabled = (kv.Value as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(flagName)) continue;
                    if (enabled != null && enabled.Equals("false", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine(SqlEmit.Replace("status_db_flag",
                        new[] { "status_name", "category", "flag_name" },
                        new object?[] { name, category, flagName }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8f: battleground + elemental
// ============================================================================

/// <summary>
/// <c>db/battleground_db.yml</c> → battleground_type_db +
/// battleground_job_restriction_db + battleground_location_db
/// (with per-team RespawnX/Y/Quit/Active/Variable flattened).
/// </summary>
public sealed class BattlegroundTypeRenormConverter : IYamlToSqlConverter
{
    public string Name => "battleground_db";
    public string SourceYamlPath => "db/battleground_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_battleground_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("battleground_type_db",
                new[] { "id", "name", "min_players", "min_level" },
                new object?[] {
                    id.Value, row.Str("Name") ?? "",
                    row.Int("MinPlayers") ?? 1, row.Int("MinLevel") ?? 1
                }));
            n++;
            if (row.Get("JobRestrictions") is YamlMappingNode jr)
            {
                foreach (var kv in jr.Children)
                {
                    var job = (kv.Key as YamlScalarNode)?.Value;
                    var enabled = (kv.Value as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(job)) continue;
                    if (enabled != null && enabled.Equals("false", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine(SqlEmit.Replace("battleground_job_restriction_db",
                        new[] { "bg_id", "job_aegis" },
                        new object?[] { id.Value, job }));
                }
            }
            foreach (var loc in row.Rows("Locations"))
            {
                var map = loc.Str("Map");
                if (string.IsNullOrEmpty(map)) continue;
                var teamA = loc.Get("TeamA") as YamlMappingNode;
                var teamB = loc.Get("TeamB") as YamlMappingNode;
                sb.AppendLine(SqlEmit.Replace("battleground_location_db",
                    new[] { "bg_id", "map_name", "start_event",
                            "team_a_respawn_x", "team_a_respawn_y", "team_a_quit_event", "team_a_active_event", "team_a_variable",
                            "team_b_respawn_x", "team_b_respawn_y", "team_b_quit_event", "team_b_active_event", "team_b_variable" },
                    new object?[] {
                        id.Value, map, loc.Str("StartEvent"),
                        teamA?.Int("RespawnX") ?? 0, teamA?.Int("RespawnY") ?? 0,
                        teamA?.Str("QuitEvent"), teamA?.Str("ActiveEvent"), teamA?.Str("Variable"),
                        teamB?.Int("RespawnX") ?? 0, teamB?.Int("RespawnY") ?? 0,
                        teamB?.Str("QuitEvent"), teamB?.Str("ActiveEvent"), teamB?.Str("Variable"),
                    }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/elemental_db.yml</c> → elemental_catalog_db + elemental_mode_db
/// (3 modes: Passive / Assist / Aggressive, each one carries Skill name).
/// </summary>
public sealed class ElementalCatalogRenormConverter : IYamlToSqlConverter
{
    public string Name => "elemental_db";
    public string SourceYamlPath => "db/re/elemental_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_elemental_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("elemental_catalog_db",
                new[] { "id", "aegis_name", "name", "level", "size", "element", "element_level" },
                new object?[] {
                    id.Value, row.Str("AegisName") ?? "", row.Str("Name") ?? "",
                    row.Int("Level") ?? 1, row.Str("Size") ?? "Small",
                    row.Str("Element") ?? "Neutral", row.Int("ElementLevel") ?? 1
                }));
            n++;
            if (row.Get("Mode") is YamlMappingNode modeMap)
            {
                foreach (var kv in modeMap.Children)
                {
                    var mode = (kv.Key as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(mode)) continue;
                    var skill = (kv.Value as YamlMappingNode)?.Str("Skill");
                    if (string.IsNullOrEmpty(skill)) continue;
                    sb.AppendLine(SqlEmit.Replace("elemental_mode_db",
                        new[] { "elemental_id", "mode", "skill_aegis" },
                        new object?[] { id.Value, mode, skill }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8g: enchant pipeline
// ============================================================================

/// <summary><c>db/re/item_enchant.yml</c> → 5 child tables.</summary>
public sealed class ItemEnchantRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_enchant";
    public string SourceYamlPath => "db/re/item_enchant.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_enchant.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id == null) continue;
            var reset = row.Get("Reset") as YamlMappingNode;
            sb.AppendLine(SqlEmit.Replace("item_enchant_db",
                new[] { "enchant_id", "minimum_refine", "reset_chance", "reset_price" },
                new object?[] {
                    id.Value, row.Int("MinimumRefine") ?? 0,
                    reset?.Int("Chance") ?? 0, reset?.Int("Price") ?? 0
                }));
            n++;
            // TargetItems: { ItemAegis: true, … }
            if (row.Get("TargetItems") is YamlMappingNode targets)
            {
                foreach (var kv in targets.Children)
                {
                    var item = (kv.Key as YamlScalarNode)?.Value;
                    var enabled = (kv.Value as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(item)) continue;
                    if (enabled != null && enabled.Equals("false", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_enchant_target_db",
                        new[] { "enchant_id", "item_aegis" },
                        new object?[] { id.Value, item }));
                }
            }
            // Reset.Materials → material rows with slot=-1.
            if (reset != null)
            {
                foreach (var mat in reset.Rows("Materials"))
                {
                    var matName = mat.Str("Material");
                    if (string.IsNullOrEmpty(matName)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_enchant_material_db",
                        new[] { "enchant_id", "slot", "material_aegis", "amount" },
                        new object?[] { id.Value, -1, matName, mat.Int("Amount") ?? 1 }));
                }
            }
            // Order: [{Slot: 3}, {Slot: 2}, …] → per-slot order index.
            var orderIndex = new Dictionary<int, int>();
            if (row.Get("Order") is YamlSequenceNode orderSeq)
            {
                var idx = 0;
                foreach (var o in orderSeq.Children.OfType<YamlMappingNode>())
                {
                    var slot = o.Int("Slot");
                    if (slot == null) continue;
                    orderIndex[slot.Value] = idx++;
                }
            }
            // Slots: per-slot Price, Materials, Enchants(by grade).
            foreach (var slot in row.Rows("Slots"))
            {
                var slotNum = slot.Int("Slot");
                if (slotNum == null) continue;
                sb.AppendLine(SqlEmit.Replace("item_enchant_slot_db",
                    new[] { "enchant_id", "slot", "price", "order_index" },
                    new object?[] {
                        id.Value, slotNum.Value, slot.Int("Price") ?? 0,
                        orderIndex.TryGetValue(slotNum.Value, out var oi) ? (int?)oi : null
                    }));
                foreach (var mat in slot.Rows("Materials"))
                {
                    var matName = mat.Str("Material");
                    if (string.IsNullOrEmpty(matName)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_enchant_material_db",
                        new[] { "enchant_id", "slot", "material_aegis", "amount" },
                        new object?[] { id.Value, slotNum.Value, matName, mat.Int("Amount") ?? 1 }));
                }
                foreach (var enchGroup in slot.Rows("Enchants"))
                {
                    var grade = enchGroup.Int("Enchantgrade") ?? 0;
                    foreach (var opt in enchGroup.Rows("Items"))
                    {
                        var item = opt.Str("Item");
                        if (string.IsNullOrEmpty(item)) continue;
                        sb.AppendLine(SqlEmit.Replace("item_enchant_option_db",
                            new[] { "enchant_id", "slot", "enchant_grade", "option_item_aegis", "chance" },
                            new object?[] { id.Value, slotNum.Value, grade, item, opt.Int("Chance") }));
                    }
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/item_reform.yml</c> → item_reform_db + item_reform_base_db.</summary>
public sealed class ItemReformRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_reform";
    public string SourceYamlPath => "db/re/item_reform.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_reform.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var result = row.Str("Item");
            if (string.IsNullOrEmpty(result)) continue;
            sb.AppendLine(SqlEmit.Replace("item_reform_db",
                new[] { "result_item_aegis" }, new object?[] { result }));
            n++;
            foreach (var b in row.Rows("BaseItems"))
            {
                var baseItem = b.Str("BaseItem");
                if (string.IsNullOrEmpty(baseItem)) continue;
                sb.AppendLine(SqlEmit.Replace("item_reform_base_db",
                    new[] { "result_item_aegis", "base_item_aegis", "maximum_refine",
                            "change_refine", "result_item_override", "random_option_group",
                            "clear_slots", "remove_enchantgrade", "cards_allowed" },
                    new object?[] {
                        result, baseItem,
                        b.Int("MaximumRefine"), b.Int("ChangeRefine"),
                        b.Str("ResultItem"), b.Str("RandomOptionGroup"),
                        b.Bool("ClearSlots") ?? false,
                        b.Bool("RemoveEnchantgrade") ?? false,
                        b.Bool("CardsAllowed") ?? true
                    }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/laphine_synthesis.yml</c> → laphine_synthesis_db + laphine_synthesis_requirement_db.</summary>
public sealed class LaphineSynthesisRenormConverter : IYamlToSqlConverter
{
    public string Name => "laphine_synthesis";
    public string SourceYamlPath => "db/re/laphine_synthesis.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_laphine_synthesis.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var item = row.Str("Item");
            if (string.IsNullOrEmpty(item)) continue;
            sb.AppendLine(SqlEmit.Replace("laphine_synthesis_db",
                new[] { "recipe_item", "reward_group", "required_requirements_count" },
                new object?[] {
                    item, row.Str("RewardGroup"),
                    row.Int("RequiredRequirementsCount") ?? 1
                }));
            n++;
            var minRefine = row.Int("MinimumRefine");
            var maxRefine = row.Int("MaximumRefine");
            foreach (var req in row.Rows("Requirements"))
            {
                var reqItem = req.Str("Item");
                if (string.IsNullOrEmpty(reqItem)) continue;
                sb.AppendLine(SqlEmit.Replace("laphine_synthesis_requirement_db",
                    new[] { "recipe_item", "requirement_item", "refine_min", "refine_max" },
                    new object?[] { item, reqItem, minRefine, maxRefine }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/laphine_upgrade.yml</c> → laphine_upgrade_db + laphine_upgrade_target_db.</summary>
public sealed class LaphineUpgradeRenormConverter : IYamlToSqlConverter
{
    public string Name => "laphine_upgrade";
    public string SourceYamlPath => "db/re/laphine_upgrade.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_laphine_upgrade.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var item = row.Str("Item");
            if (string.IsNullOrEmpty(item)) continue;
            // RandomOptionGroup is a name string in the YAML; the DB
            // column is int? (FK to item_randomopt_group_db.id). Without
            // a name→id resolver we leave it NULL — runtime can lookup
            // by name via a JOIN.
            sb.AppendLine(SqlEmit.Replace("laphine_upgrade_db",
                new[] { "upgrade_item", "random_option_group", "minimum_refine" },
                new object?[] { item, null, row.Int("MinimumRefine") }));
            n++;
            foreach (var t in row.Rows("TargetItems"))
            {
                var target = t.Str("Item");
                if (string.IsNullOrEmpty(target)) continue;
                sb.AppendLine(SqlEmit.Replace("laphine_upgrade_target_db",
                    new[] { "upgrade_item", "target_item" },
                    new object?[] { item, target }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/item_randomopt_group.yml</c> →
/// item_randomopt_group_db + item_randomopt_group_option_db
/// (flatten Slots[*].Options[*] into one row per (group, slot, option)).
/// </summary>
public sealed class ItemRandomOptGroupRenormConverter : IYamlToSqlConverter
{
    public string Name => "item_randomopt_group";
    public string SourceYamlPath => "db/re/item_randomopt_group.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_randomopt_group.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("item_randomopt_group_db",
                new[] { "id", "group_name" },
                new object?[] { id.Value, row.Str("Group") ?? "" }));
            n++;
            foreach (var slot in row.Rows("Slots"))
            {
                var slotNum = slot.Int("Slot");
                if (slotNum == null) continue;
                foreach (var opt in slot.Rows("Options"))
                {
                    var optName = opt.Str("Option");
                    if (string.IsNullOrEmpty(optName)) continue;
                    sb.AppendLine(SqlEmit.Replace("item_randomopt_group_option_db",
                        new[] { "group_id", "slot", "option_name",
                                "min_value", "max_value", "chance" },
                        new object?[] {
                            id.Value, slotNum.Value, optName,
                            opt.Int("MinValue") ?? 0, opt.Int("MaxValue") ?? 0,
                            opt.Int("Chance") ?? 0
                        }));
                }
            }
            // Random[] (unbounded slot — emit under synthetic slot 99).
            foreach (var opt in row.Rows("Random"))
            {
                var optName = opt.Str("Option");
                if (string.IsNullOrEmpty(optName)) continue;
                sb.AppendLine(SqlEmit.Replace("item_randomopt_group_option_db",
                    new[] { "group_id", "slot", "option_name",
                            "min_value", "max_value", "chance" },
                    new object?[] {
                        id.Value, 99, optName,
                        opt.Int("MinValue") ?? 0, opt.Int("MaxValue") ?? 0,
                        opt.Int("Chance") ?? 0
                    }));
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8h: refine + enchantgrade
// ============================================================================

/// <summary><c>db/re/refine.yml</c> → refine_group_db + refine_level_db + refine_chance_db.</summary>
public sealed class RefineRenormConverter : IYamlToSqlConverter
{
    public string Name => "refine";
    public string SourceYamlPath => "db/re/refine.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_refine.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var group = row.Str("Group");
            if (string.IsNullOrEmpty(group)) continue;
            sb.AppendLine(SqlEmit.Replace("refine_group_db",
                new[] { "group_name" }, new object?[] { group }));
            n++;
            foreach (var lvl in row.Rows("Levels"))
            {
                var itemLvl = lvl.Int("Level");
                if (itemLvl == null) continue;
                foreach (var rLvl in lvl.Rows("RefineLevels"))
                {
                    var refineLvl = rLvl.Int("Level");
                    var bonus = rLvl.Int("Bonus") ?? 0;
                    if (refineLvl == null) continue;
                    sb.AppendLine(SqlEmit.Replace("refine_level_db",
                        new[] { "group_name", "item_level", "refine_level", "bonus" },
                        new object?[] { group, itemLvl.Value, refineLvl.Value, bonus }));
                    foreach (var ch in rLvl.Rows("Chances"))
                    {
                        var type = ch.Str("Type");
                        if (string.IsNullOrEmpty(type)) continue;
                        sb.AppendLine(SqlEmit.Replace("refine_chance_db",
                            new[] { "group_name", "item_level", "refine_level", "chance_type",
                                    "rate", "price", "material_aegis" },
                            new object?[] {
                                group, itemLvl.Value, refineLvl.Value, type,
                                ch.Int("Rate") ?? 0, ch.Int("Price") ?? 0,
                                ch.Str("Material") ?? ""
                            }));
                    }
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary><c>db/re/enchantgrade.yml</c> → enchantgrade_db + enchantgrade_level_db + enchantgrade_chance_db.</summary>
public sealed class EnchantGradeRenormConverter : IYamlToSqlConverter
{
    public string Name => "enchantgrade";
    public string SourceYamlPath => "db/re/enchantgrade.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_enchantgrade.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var type = row.Str("Type");
            if (string.IsNullOrEmpty(type)) continue;
            sb.AppendLine(SqlEmit.Replace("enchantgrade_db",
                new[] { "equip_type" }, new object?[] { type }));
            n++;
            foreach (var lvl in row.Rows("Levels"))
            {
                var itemLvl = lvl.Int("Level");
                if (itemLvl == null) continue;
                foreach (var g in lvl.Rows("Grades"))
                {
                    var grade = g.Str("Grade");
                    if (string.IsNullOrEmpty(grade)) continue;
                    sb.AppendLine(SqlEmit.Replace("enchantgrade_level_db",
                        new[] { "equip_type", "item_level", "grade" },
                        new object?[] { type, itemLvl.Value, grade }));
                    foreach (var ch in g.Rows("Chances"))
                    {
                        var refine = ch.Int("Refine");
                        var chance = ch.Int("Chance");
                        if (refine == null || chance == null) continue;
                        sb.AppendLine(SqlEmit.Replace("enchantgrade_chance_db",
                            new[] { "equip_type", "item_level", "grade", "refine", "chance" },
                            new object?[] { type, itemLvl.Value, grade, refine.Value, chance.Value }));
                    }
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// DB-8i: drop overrides
// ============================================================================

/// <summary>
/// <c>db/re/map_drops.yml</c> → map_drop_db + map_drop_entry_db
/// (GlobalDrops + SpecificDrops merged; per-mob filter set on the entry).
/// </summary>
public sealed class MapDropsRenormConverter : IYamlToSqlConverter
{
    public string Name => "map_drops";
    public string SourceYamlPath => "db/re/map_drops.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_map_drops.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var map = row.Str("Map");
            if (string.IsNullOrEmpty(map)) continue;
            sb.AppendLine(SqlEmit.Replace("map_drop_db",
                new[] { "map_name" }, new object?[] { map }));
            n++;
            var entryIdx = 0;
            foreach (var d in row.Rows("GlobalDrops"))
            {
                var item = d.Str("Item");
                if (string.IsNullOrEmpty(item)) continue;
                sb.AppendLine(SqlEmit.Replace("map_drop_entry_db",
                    new[] { "map_name", "entry_index", "item_aegis", "rate", "mob_filter_aegis" },
                    new object?[] { map, entryIdx++, item, d.Int("Rate") ?? 0, null }));
            }
            foreach (var s in row.Rows("SpecificDrops"))
            {
                var monster = s.Str("Monster");
                foreach (var d in s.Rows("Drops"))
                {
                    var item = d.Str("Item");
                    if (string.IsNullOrEmpty(item)) continue;
                    sb.AppendLine(SqlEmit.Replace("map_drop_entry_db",
                        new[] { "map_name", "entry_index", "item_aegis", "rate", "mob_filter_aegis" },
                        new object?[] { map, entryIdx++, item, d.Int("Rate") ?? 0, monster }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/mob_item_ratio.yml</c> → mob_item_ratio_db + mob_item_ratio_mob_db.
/// Body is typically empty in stock rAthena (footer-only) — emits an
/// empty seed which the runtime treats as "no overrides".
/// </summary>
public sealed class MobItemRatioRenormConverter : IYamlToSqlConverter
{
    public string Name => "mob_item_ratio";
    public string SourceYamlPath => "db/mob_item_ratio.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mob_item_ratio.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var item = row.Str("Item");
            if (string.IsNullOrEmpty(item)) continue;
            sb.AppendLine(SqlEmit.Replace("mob_item_ratio_db",
                new[] { "item_aegis", "ratio" },
                new object?[] { item, row.Int("Ratio") ?? 100 }));
            n++;
            if (row.Get("List") is YamlMappingNode mobs)
            {
                foreach (var kv in mobs.Children)
                {
                    var mob = (kv.Key as YamlScalarNode)?.Value;
                    var enabled = (kv.Value as YamlScalarNode)?.Value;
                    if (string.IsNullOrEmpty(mob)) continue;
                    if (enabled != null && enabled.Equals("false", StringComparison.OrdinalIgnoreCase)) continue;
                    sb.AppendLine(SqlEmit.Replace("mob_item_ratio_mob_db",
                        new[] { "item_aegis", "mob_aegis" },
                        new object?[] { item, mob }));
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// AT-F: nested child tables (mercenary_db.Skills + homunculus_db.SkillTree)
// ============================================================================

/// <summary>
/// <c>db/re/mercenary_db.yml</c> nested Skills:[] →
/// mercenary_skill_db. Separate emit (the merc_db parent rows are
/// generated by MercenaryDbConverter).
/// </summary>
public sealed class MercenarySkillRenormConverter : IYamlToSqlConverter
{
    public string Name => "mercenary_skill_db";
    public string SourceYamlPath => "db/re/mercenary_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mercenary_skill_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var mercId = row.Int("Id");
            if (mercId == null) continue;
            foreach (var s in row.Rows("Skills"))
            {
                var aegis = s.Str("Name");
                if (string.IsNullOrEmpty(aegis)) continue;
                // SkillId is not in the yml — set to 0; runtime resolves
                // by name via skill_db. The composite key tolerates this.
                sb.AppendLine(SqlEmit.Replace("mercenary_skill_db",
                    new[] { "merc_id", "skill_id", "skill_aegis", "max_level" },
                    new object?[] {
                        (uint)mercId.Value, (ushort)0, aegis,
                        (ushort)(s.Int("MaxLevel") ?? 1)
                    }));
                n++;
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/homunculus_db.yml</c> nested SkillTree:[] →
/// homunculus_skill_tree_db. Separate emit (the homunculus_db parent
/// rows are generated by HomunculusDbConverter).
/// </summary>
public sealed class HomunculusSkillTreeRenormConverter : IYamlToSqlConverter
{
    public string Name => "homunculus_skill_tree_db";
    public string SourceYamlPath => "db/re/homunculus_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_homunculus_skill_tree_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var classAegis = row.Str("Class");
            if (string.IsNullOrEmpty(classAegis)) continue;
            foreach (var s in row.Rows("SkillTree"))
            {
                var skill = s.Str("Skill");
                if (string.IsNullOrEmpty(skill)) continue;
                var requiredLevel = (ushort)0;
                // Required:[{Skill: X, Level: N}] — the first entry's Level
                // becomes the required base; later required-skill chaining
                // lives in another table (none for homun in stock yml).
                foreach (var req in s.Rows("Required"))
                {
                    requiredLevel = (ushort)(req.Int("Level") ?? 0);
                    break;
                }
                sb.AppendLine(SqlEmit.Replace("homunculus_skill_tree_db",
                    new[] { "class_aegis", "skill_id", "skill_aegis", "max_level",
                            "required_level", "required_intimacy", "require_evolution" },
                    new object?[] {
                        classAegis, (ushort)0, skill,
                        (ushort)(s.Int("MaxLevel") ?? 1),
                        requiredLevel,
                        (ushort)(s.Int("RequiredIntimacy") ?? 0),
                        s.Bool("RequireEvolution") ?? false
                    }));
                n++;
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

// ============================================================================
// AT-G: stylist, achievement_level, job_aspd, const
// ============================================================================

internal static class LookEnum
{
    /// <summary>Map rAthena yml stylist Look name → look enum int.</summary>
    public static int Resolve(string? name) => name switch
    {
        "Hair"           => 1,
        "Weapon"         => 2,
        "Head_Bottom"    => 3,
        "Head_Top"       => 4,
        "Head_Mid"       => 5,
        "Hair_Color"     => 6,
        "Cloth_Color"    => 7,
        "Clothes_Color"  => 7,
        "Shield"         => 8,
        "Shoes"          => 9,
        "Body"           => 10,
        "Reset"          => 11,
        "Robe"           => 12,
        "Body2"          => 13,
        _                => 0,
    };
}

internal static class WeaponTypeEnum
{
    /// <summary>Map rAthena yml weapon name → weapon_type enum int.</summary>
    public static int Resolve(string? name) => name switch
    {
        "Fist"      => 0,
        "Dagger"    => 1,
        "1hSword"   => 2,
        "2hSword"   => 3,
        "1hSpear"   => 4,
        "2hSpear"   => 5,
        "1hAxe"     => 6,
        "2hAxe"     => 7,
        "Mace"      => 8,
        "2hMace"    => 9,
        "Staff"     => 10,
        "Bow"       => 11,
        "Knuckle"   => 12,
        "Musical"   => 13,
        "Whip"      => 14,
        "Book"      => 15,
        "Katar"     => 16,
        "Revolver"  => 17,
        "Rifle"     => 18,
        "Gatling"   => 19,
        "Shotgun"   => 20,
        "Grenade"   => 21,
        "Huuma"     => 22,
        "2hStaff"   => 23,
        "Shield"    => 99,
        _           => -1,
    };
}

/// <summary><c>db/re/stylist.yml</c> → stylist_db (one row per Look + Index, Human/Doram split).</summary>
public sealed class StylistRenormConverter : IYamlToSqlConverter
{
    public string Name => "stylist_db";
    public string SourceYamlPath => "db/re/stylist.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_stylist_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lookName = row.Str("Look");
            var look = LookEnum.Resolve(lookName);
            if (look == 0) continue;
            foreach (var opt in row.Rows("Options"))
            {
                var idx = opt.Int("Index");
                var value = opt.Int("Value");
                if (idx == null) continue;
                // CostsHuman + CostsDoram → two rows (DoramOnly is part of PK).
                EmitVariant(sb, look, idx.Value, value ?? idx.Value, opt.Get("CostsHuman") as YamlMappingNode, false, ref n);
                EmitVariant(sb, look, idx.Value, value ?? idx.Value, opt.Get("CostsDoram") as YamlMappingNode, true, ref n);
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }

    private static void EmitVariant(StringBuilder sb, int look, int idx, int value,
        YamlMappingNode? costs, bool doramOnly, ref int n)
    {
        if (costs == null) return;
        sb.AppendLine(SqlEmit.Replace("stylist_db",
            new[] { "look", "client_index", "value", "doram_only",
                    "cost_zeny", "required_item_aegis", "required_item_box_aegis" },
            new object?[] {
                look, idx, value, doramOnly,
                costs.Int("Price") ?? 0,
                costs.Str("RequiredItem"),
                costs.Str("RequiredItemBox")
            }));
        n++;
    }
}

/// <summary><c>db/re/achievement_level_db.yml</c> → achievement_level_db.</summary>
public sealed class AchievementLevelRenormConverter : IYamlToSqlConverter
{
    public string Name => "achievement_level_db";
    public string SourceYamlPath => "db/re/achievement_level_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_achievement_level_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lvl = row.Int("Level");
            var points = row.Long("Points");
            if (lvl == null || points == null) continue;
            sb.AppendLine(SqlEmit.Replace("achievement_level_db",
                new[] { "level", "required_points" },
                new object?[] { lvl.Value, points.Value }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/re/job_aspd.yml</c> → job_aspd_db. Each Body row groups multiple
/// jobs sharing a BaseASPD map (weapon_name → delay_ms).
/// </summary>
public sealed class JobAspdRenormConverter : IYamlToSqlConverter
{
    public string Name => "job_aspd_db";
    public string SourceYamlPath => "db/re/job_aspd.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_aspd_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var jobs = JobRowHelpers.EnumerateJobs(row).ToList();
            if (row.Get("BaseASPD") is not YamlMappingNode aspd) continue;
            foreach (var job in jobs)
            {
                foreach (var kv in aspd.Children)
                {
                    var weaponName = (kv.Key as YamlScalarNode)?.Value;
                    var delayStr = (kv.Value as YamlScalarNode)?.Value;
                    var wType = WeaponTypeEnum.Resolve(weaponName);
                    if (wType < 0 || delayStr == null) continue;
                    if (!int.TryParse(delayStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delay)) continue;
                    sb.AppendLine(SqlEmit.Replace("job_aspd_db",
                        new[] { "job_aegis", "weapon_type", "base_delay_ms" },
                        new object?[] { job, wType, delay }));
                    n++;
                }
            }
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

/// <summary>
/// <c>db/const.yml</c> → const_db. Values can be decimal or hex literals
/// (e.g. <c>Value: 0x00000001</c>); we parse both.
/// </summary>
public sealed class ConstRenormConverter : IYamlToSqlConverter
{
    public string Name => "const_db";
    public string SourceYamlPath => "db/const.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_const_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var name = row.Str("Name");
            var valueStr = row.Str("Value");
            if (string.IsNullOrEmpty(name) || valueStr == null) continue;
            if (!TryParseLongLiteral(valueStr, out var value)) continue;
            sb.AppendLine(SqlEmit.Replace("const_db",
                new[] { "name", "value", "is_parameter" },
                new object?[] { name, value, row.Bool("Parameter") ?? false }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }

    private static bool TryParseLongLiteral(string s, out long value)
    {
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;
        if ((s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || s.StartsWith("0X")) &&
            long.TryParse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            return true;
        value = 0;
        return false;
    }
}
