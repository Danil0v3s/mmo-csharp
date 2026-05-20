using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary><c>db/magicmushroom_db.yml</c> → <c>seed_magicmushroom_db.sql</c>.</summary>
public sealed class MagicMushroomDbConverter : IYamlToSqlConverter
{
    public string Name => "magicmushroom_db";
    public string SourceYamlPath => "db/re/magicmushroom_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_magicmushroom_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var skill = row.Str("Skill"); if (skill == null) continue;
            sb.AppendLine(SqlEmit.Replace("magicmushroom_db",
                new[] { "skill_name" }, new object?[] { skill }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
