using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/achievement_db.yml</c> → <c>seed_achievement_db.sql</c>.
/// Achievement table — id + group + name + score + targets.
/// Multi-target rows get flattened to a JSON-ish summary column
/// (the runtime parses it back); the per-objective Mob+Count table
/// could be a separate child table — for now we keep it inline so
/// the seed stays single-file.
/// </summary>
public sealed class AchievementDbConverter : IYamlToSqlConverter
{
    public string Name => "achievement_db";
    public string SourceYamlPath => "db/re/achievement_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_achievement_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            var grp = row.Str("Group") ?? "";
            var name = row.Str("Name") ?? "";
            var score = row.Int("Score") ?? 0;
            var dep = row.Get("Dependents") is YamlDotNet.RepresentationModel.YamlSequenceNode dep1
                ? string.Join(',', dep1.Children.OfType<YamlDotNet.RepresentationModel.YamlMappingNode>().Select(c => c.Str("Id")).Where(s => s != null)) : "";

            // Targets — flatten to "mobName1:count1;mobName2:count2;..."
            var targetSb = new StringBuilder();
            foreach (var t in row.Rows("Targets"))
            {
                if (targetSb.Length > 0) targetSb.Append(';');
                var mob = t.Str("Mob");
                if (mob != null)
                {
                    targetSb.Append(mob).Append(':').Append(t.Int("Count") ?? 0);
                }
                else
                {
                    targetSb.Append("@id=").Append(t.Int("Id") ?? 0).Append(':').Append(t.Int("Count") ?? 0);
                }
            }

            sb.AppendLine(SqlEmit.Replace("achievement_db",
                new[] { "achievement_id", "group_name", "name", "score", "dependents", "targets" },
                new object?[] { (uint)id.Value, grp, name, score, dep, targetSb.ToString() }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
