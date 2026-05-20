using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/abra_db.yml</c> → <c>seed_abra_db.sql</c>.
/// Abracadabra pool — single column: SkillName.
/// </summary>
public sealed class AbraDbConverter : IYamlToSqlConverter
{
    public string Name => "abra_db";
    public string SourceYamlPath => "db/abra_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_abra_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var src = Path.Combine(rathenaRoot, SourceYamlPath);
        var body = YamlHelpers.LoadBody(src);
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var skill = row.Str("Skill"); if (skill == null) continue;
            sb.AppendLine(SqlEmit.Replace("abra_db",
                new[] { "skill_name" }, new object?[] { skill }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
