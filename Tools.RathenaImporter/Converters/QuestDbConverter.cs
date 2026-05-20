using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/quest_db.yml</c> → <c>seed_quest_db.sql</c>. Pulls
/// Id + Title + TimeLimit + the per-quest Targets list (flattened to
/// up to 3 mob targets per row, matching rAthena's `mob1..mob3`
/// schema in their old SQL exporter).
/// </summary>
public sealed class QuestDbConverter : IYamlToSqlConverter
{
    public string Name => "quest_db";
    public string SourceYamlPath => "db/re/quest_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_quest_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            var title = row.Str("Title") ?? "";
            var timeLimit = row.Str("TimeLimit") ?? "";

            // Targets — up to 3 (matches Quest UI). Each target has
            // Mob + Count, plus optional Id (for "kill mob from rg").
            string? m1 = null, m2 = null, m3 = null;
            int c1 = 0, c2 = 0, c3 = 0;
            var i = 0;
            foreach (var t in row.Rows("Targets"))
            {
                var mob = t.Str("Mob");
                var count = t.Int("Count") ?? 0;
                if (i == 0) { m1 = mob; c1 = count; }
                else if (i == 1) { m2 = mob; c2 = count; }
                else if (i == 2) { m3 = mob; c3 = count; }
                i++;
                if (i >= 3) break;
            }

            sb.AppendLine(SqlEmit.Replace("quest_db",
                new[] {
                    "quest_id", "title", "time_limit",
                    "mob1", "count1", "mob2", "count2", "mob3", "count3",
                },
                new object?[] {
                    (uint)id.Value, title, timeLimit,
                    m1, c1, m2, c2, m3, c3,
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
