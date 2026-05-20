using System.Text;
using YamlDotNet.RepresentationModel;

namespace Tools.RathenaImporter.Converters;

// "Payload" converters — rAthena tables with deeply nested YAML
// structures we don't want to fully normalize into many SQL tables.
// We pick a primary key column out of the row and stuff the rest of
// the YAML as `payload_json` text. The runtime service deserializes
// the JSON into a typed record on Reload().
//
// Trade-off: lossy in the sense that the data isn't queryable column-
// by-column; lossless in the sense that every field round-trips.
// rAthena loads these tables in-memory at boot anyway, so this matches
// how the live server uses the data.

internal static class PayloadConverter
{
    /// <summary>
    /// Generic shape: extract <paramref name="keyField"/> as the primary
    /// key, serialize the full row to JSON. Skips rows where the key is
    /// missing.
    /// </summary>
    public static string EmitByStringKey(YamlSequenceNode body, string table, string keyColumn, string keyField, string converterName, string source)
    {
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var key = row.Str(keyField);
            if (string.IsNullOrEmpty(key)) continue;
            sb.AppendLine(SqlEmit.Replace(table,
                new[] { keyColumn, "payload_json" },
                new object?[] { key, YamlHelpers.ToJson(row) }));
            n++;
        }
        return SqlEmit.Header(converterName, source, n) + sb;
    }

    public static string EmitByIntKey(YamlSequenceNode body, string table, string keyColumn, string keyField, string converterName, string source)
    {
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var key = row.Int(keyField);
            if (key == null) continue;
            sb.AppendLine(SqlEmit.Replace(table,
                new[] { keyColumn, "payload_json" },
                new object?[] { key.Value, YamlHelpers.ToJson(row) }));
            n++;
        }
        return SqlEmit.Header(converterName, source, n) + sb;
    }

    /// <summary>
    /// For tables that have no obvious key — number rows 1..N.
    /// </summary>
    public static string EmitByRowIndex(YamlSequenceNode body, string table, string converterName, string source)
    {
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            n++;
            sb.AppendLine(SqlEmit.Replace(table,
                new[] { "row_id", "payload_json" },
                new object?[] { n, YamlHelpers.ToJson(row) }));
        }
        return SqlEmit.Header(converterName, source, n) + sb;
    }
}

public sealed class ElementalDbConverter : IYamlToSqlConverter
{
    public string Name => "elemental_db";
    public string SourceYamlPath => "db/re/elemental_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_elemental_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "elemental_db", "ele_id", "Id", Name, SourceYamlPath));
}

public sealed class BattlegroundDbConverter : IYamlToSqlConverter
{
    public string Name => "battleground_db";
    public string SourceYamlPath => "db/battleground_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_battleground_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "battleground_db", "bg_id", "Id", Name, SourceYamlPath));
}

public sealed class SkillTreeConverter : IYamlToSqlConverter
{
    public string Name => "skill_tree";
    public string SourceYamlPath => "db/re/skill_tree.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_skill_tree.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "skill_tree", "job_name", "Job", Name, SourceYamlPath));
}

public sealed class GuildSkillTreeConverter : IYamlToSqlConverter
{
    public string Name => "guild_skill_tree";
    public string SourceYamlPath => "db/re/guild_skill_tree.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_guild_skill_tree.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "guild_skill_tree", "skill_name", "Id", Name, SourceYamlPath));
}

public sealed class MobSummonConverter : IYamlToSqlConverter
{
    public string Name => "mob_summon";
    public string SourceYamlPath => "db/re/mob_summon.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mob_summon.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "mob_summon", "group_name", "Group", Name, SourceYamlPath));
}

public sealed class ItemRandomOptGroupConverter : IYamlToSqlConverter
{
    public string Name => "item_randomopt_group";
    public string SourceYamlPath => "db/re/item_randomopt_group.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_randomopt_group.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_randomopt_group", "group_id", "Id", Name, SourceYamlPath));
}

public sealed class AttrFixConverter : IYamlToSqlConverter
{
    public string Name => "attr_fix";
    public string SourceYamlPath => "db/re/attr_fix.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_attr_fix.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "attr_fix", "ele_level", "Level", Name, SourceYamlPath));
}

public sealed class LevelPenaltyConverter : IYamlToSqlConverter
{
    public string Name => "level_penalty";
    public string SourceYamlPath => "db/re/level_penalty.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_level_penalty.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "level_penalty", "penalty_type", "Type", Name, SourceYamlPath));
}

public sealed class JobStatsConverter : IYamlToSqlConverter
{
    public string Name => "job_stats";
    public string SourceYamlPath => "db/re/job_stats.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_stats.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByRowIndex(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "job_stats", Name, SourceYamlPath));
}

public sealed class JobExpConverter : IYamlToSqlConverter
{
    public string Name => "job_exp";
    public string SourceYamlPath => "db/re/job_exp.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_exp.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByRowIndex(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "job_exp", Name, SourceYamlPath));
}

public sealed class JobBasePointsConverter : IYamlToSqlConverter
{
    public string Name => "job_basepoints";
    public string SourceYamlPath => "db/re/job_basepoints.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_job_basepoints.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByRowIndex(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "job_basepoints", Name, SourceYamlPath));
}

public sealed class StatusYmlConverter : IYamlToSqlConverter
{
    public string Name => "status_yml";
    public string SourceYamlPath => "db/re/status.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_status_yml.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "status_yml", "status_name", "Status", Name, SourceYamlPath));
}

public sealed class ItemCombosConverter : IYamlToSqlConverter
{
    public string Name => "item_combos";
    public string SourceYamlPath => "db/re/item_combos.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_combos.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByRowIndex(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_combos", Name, SourceYamlPath));
}

public sealed class ItemPackagesConverter : IYamlToSqlConverter
{
    public string Name => "item_packages";
    public string SourceYamlPath => "db/re/item_packages.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_packages.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_packages", "item_aegis", "Item", Name, SourceYamlPath));
}

public sealed class ItemGroupDbConverter : IYamlToSqlConverter
{
    public string Name => "item_group_db";
    public string SourceYamlPath => "db/re/item_group_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_group_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_group_db", "group_name", "Group", Name, SourceYamlPath));
}

public sealed class ItemEnchantConverter : IYamlToSqlConverter
{
    public string Name => "item_enchant";
    public string SourceYamlPath => "db/re/item_enchant.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_enchant.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_enchant", "enchant_id", "Id", Name, SourceYamlPath));
}

public sealed class ItemReformConverter : IYamlToSqlConverter
{
    public string Name => "item_reform";
    public string SourceYamlPath => "db/re/item_reform.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_reform.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_reform", "item_aegis", "Item", Name, SourceYamlPath));
}

public sealed class LaphineSynthesisConverter : IYamlToSqlConverter
{
    public string Name => "laphine_synthesis";
    public string SourceYamlPath => "db/re/laphine_synthesis.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_laphine_synthesis.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "laphine_synthesis", "recipe_item", "Item", Name, SourceYamlPath));
}

public sealed class LaphineUpgradeConverter : IYamlToSqlConverter
{
    public string Name => "laphine_upgrade";
    public string SourceYamlPath => "db/re/laphine_upgrade.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_laphine_upgrade.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "laphine_upgrade", "upgrade_item", "Item", Name, SourceYamlPath));
}

public sealed class RefineConverter : IYamlToSqlConverter
{
    public string Name => "refine";
    public string SourceYamlPath => "db/re/refine.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_refine.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "refine", "refine_group", "Group", Name, SourceYamlPath));
}

public sealed class EnchantGradeConverter : IYamlToSqlConverter
{
    public string Name => "enchantgrade";
    public string SourceYamlPath => "db/re/enchantgrade.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_enchantgrade.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "enchantgrade", "equip_type", "Type", Name, SourceYamlPath));
}

public sealed class MapDropsConverter : IYamlToSqlConverter
{
    public string Name => "map_drops";
    public string SourceYamlPath => "db/re/map_drops.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_map_drops.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "map_drops", "map_name", "Map", Name, SourceYamlPath));
}

public sealed class MobItemRatioConverter : IYamlToSqlConverter
{
    public string Name => "mob_item_ratio";
    public string SourceYamlPath => "db/mob_item_ratio.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mob_item_ratio.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByStringKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "mob_item_ratio", "item_aegis", "Item", Name, SourceYamlPath));
}

public sealed class ItemCashConverter : IYamlToSqlConverter
{
    public string Name => "item_cash";
    public string SourceYamlPath => "db/item_cash.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_cash.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByRowIndex(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "item_cash", Name, SourceYamlPath));
}

public sealed class AttendanceConverter : IYamlToSqlConverter
{
    public string Name => "attendance";
    public string SourceYamlPath => "db/re/attendance.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_attendance_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "attendance_db", "attendance_id", "Id", Name, SourceYamlPath));
}

public sealed class ReputationGroupConverter : IYamlToSqlConverter
{
    public string Name => "reputation_group";
    public string SourceYamlPath => "db/re/reputation_group.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_reputation_group.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
        => Task.FromResult(PayloadConverter.EmitByIntKey(
            YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath)),
            "reputation_group", "group_id", "Id", Name, SourceYamlPath));
}
