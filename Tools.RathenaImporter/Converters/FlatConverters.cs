using System.Text;

namespace Tools.RathenaImporter.Converters;

// Flat-shape converters — YAML rows fit into a single SQL row with
// no nested structures lost. One converter per table; everything
// bundled here to avoid one-file-per-class noise.

public sealed class CastleDbConverter : IYamlToSqlConverter
{
    public string Name => "castle_db";
    public string SourceYamlPath => "db/re/castle_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_castle_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("castle_db",
                new[] { "castle_id", "map_name", "name", "npc_name", "castle_type", "client_id", "warp_enabled", "warp_x", "warp_y" },
                new object?[] { (uint)id.Value, row.Str("Map"), row.Str("Name"), row.Str("Npc"), row.Str("Type"), row.Int("ClientId"), row.Bool("WarpEnabled"), row.Int("WarpX"), row.Int("WarpY") }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class StatPointConverter : IYamlToSqlConverter
{
    public string Name => "statpoint";
    public string SourceYamlPath => "db/re/statpoint.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_statpoint.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lvl = row.Int("Level"); if (lvl == null) continue;
            sb.AppendLine(SqlEmit.Replace("statpoint",
                new[] { "level", "points", "trait_points" },
                new object?[] { lvl.Value, row.Int("Points") ?? 0, row.Int("TraitPoints") ?? 0 }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class ExpHomunConverter : IYamlToSqlConverter
{
    public string Name => "exp_homun";
    public string SourceYamlPath => "db/re/exp_homun.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_exp_homun.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lvl = row.Int("Level"); if (lvl == null) continue;
            sb.AppendLine(SqlEmit.Replace("exp_homun",
                new[] { "level", "exp" },
                new object?[] { lvl.Value, row.Long("Exp") ?? 0 }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class ExpGuildConverter : IYamlToSqlConverter
{
    public string Name => "exp_guild";
    public string SourceYamlPath => "db/re/exp_guild.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_exp_guild.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var lvl = row.Int("Level"); if (lvl == null) continue;
            sb.AppendLine(SqlEmit.Replace("exp_guild",
                new[] { "level", "exp" },
                new object?[] { lvl.Value, row.Long("Exp") ?? 0 }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class SizeFixConverter : IYamlToSqlConverter
{
    public string Name => "size_fix";
    public string SourceYamlPath => "db/re/size_fix.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_size_fix.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var weapon = row.Str("Weapon"); if (weapon == null) continue;
            sb.AppendLine(SqlEmit.Replace("size_fix",
                new[] { "weapon", "small", "medium", "large" },
                new object?[] { weapon, row.Int("Small") ?? 100, row.Int("Medium") ?? 100, row.Int("Large") ?? 100 }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class ReputationConverter : IYamlToSqlConverter
{
    public string Name => "reputation";
    public string SourceYamlPath => "db/re/reputation.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_reputation.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("reputation",
                new[] { "reputation_id", "name", "variable", "minimum", "maximum" },
                new object?[] { id.Value, row.Str("Name"), row.Str("Variable"), row.Int("Minimum") ?? 0, row.Int("Maximum") ?? 0 }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class CreateArrowDbConverter : IYamlToSqlConverter
{
    public string Name => "create_arrow_db";
    public string SourceYamlPath => "db/create_arrow_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_create_arrow_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var src = row.Str("Source"); if (src == null) continue;
            // Make is a nested list; flatten to JSON to keep all recipes in one row.
            var make = row.Get("Make");
            var makeJson = make != null ? YamlHelpers.ToJson(make) : "[]";
            sb.AppendLine(SqlEmit.Replace("create_arrow_db",
                new[] { "source_item", "make_json" },
                new object?[] { src, makeJson }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class ItemRandomOptDbConverter : IYamlToSqlConverter
{
    public string Name => "item_randomopt_db";
    public string SourceYamlPath => "db/re/item_randomopt_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_item_randomopt_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("item_randomopt_db",
                new[] { "option_id", "option_name", "script" },
                new object?[] { id.Value, row.Str("Option"), row.Str("Script") }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class CashShopDbConverter : IYamlToSqlConverter
{
    public string Name => "cashshop_db";
    public string SourceYamlPath => "db/cashshop_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_cashshop_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            // Cash shop entries are grouped per Tab; each Tab has Items list.
            var tab = row.Str("Tab"); if (tab == null) continue;
            var itemsJson = row.Get("Items") is YamlDotNet.RepresentationModel.YamlNode items
                ? YamlHelpers.ToJson(items) : "[]";
            sb.AppendLine(SqlEmit.Replace("cashshop_db",
                new[] { "tab", "items_json" },
                new object?[] { tab, itemsJson }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}

public sealed class CaptchaDbConverter : IYamlToSqlConverter
{
    public string Name => "captcha_db";
    public string SourceYamlPath => "db/captcha_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_captcha_db.sql";
    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("captcha_db",
                new[] { "captcha_id", "image", "answer" },
                new object?[] { (uint)id.Value, row.Str("Image"), row.Str("Answer") }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
