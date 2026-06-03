using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/quest_db.yml</c> → <c>seed_quest_db.sql</c>. Pulls Id + Title + TimeLimit + the per-quest
/// Targets list (flattened to up to 3 objectives, matching <c>MAX_QUEST_OBJECTIVES</c> / the
/// <c>mob1..mob3</c> schema). Each objective is either mob-specific (<c>Mob</c> set) or an "any-mob"
/// filter (<c>Mob</c> absent, matched by Race/Size/Element/MinLevel/MaxLevel/Location + an optional
/// MapMobTargets allow-list). Mirrors <c>s_quest_db::objectives</c> (quest.cpp).
/// </summary>
public sealed class QuestDbConverter : IYamlToSqlConverter
{
    public string Name => "quest_db";
    public string SourceYamlPath => "db/re/quest_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_quest_db.sql";

    private readonly record struct Target(
        string? Mob, int Count, string? Race, string? Size, string? Element,
        int MinLevel, int MaxLevel, string? Location, string? MobsAllowed);

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

            // Objectives — push only non-skipped targets (rAthena builds a compacted vector; a
            // Count:0 target is skipped entirely, so later targets shift up). Cap at 3.
            var obj = new Target[3];
            var slot = 0;
            foreach (var t in row.Rows("Targets"))
            {
                if (slot >= 3) break;
                var count = t.Int("Count") ?? 0;
                if (count == 0) continue; // rAthena: Count 0 skips the target on import

                // MinLevel default rule: absent but MaxLevel defined → MinLevel = 1.
                var maxLevel = t.Int("MaxLevel") ?? 0;
                var minLevel = t.Int("MinLevel") ?? (maxLevel > 0 ? 1 : 0);

                obj[slot++] = new Target(
                    Mob: t.Str("Mob"),
                    Count: count,
                    Race: t.Str("Race"),
                    Size: t.Str("Size"),
                    Element: t.Str("Element"),
                    MinLevel: minLevel,
                    MaxLevel: maxLevel,
                    Location: t.Str("Location"),
                    // MapMobTargets: { <name>: <bool> } → '|'-delimited allowed aegis names.
                    MobsAllowed: NullIfEmpty(t.TrueKeys("MapMobTargets")));
            }

            sb.AppendLine(SqlEmit.Replace("quest_db",
                new[] {
                    "quest_id", "title", "time_limit",
                    "mob1", "count1", "race1", "size1", "element1", "min_level1", "max_level1", "location1", "mobs_allowed1",
                    "mob2", "count2", "race2", "size2", "element2", "min_level2", "max_level2", "location2", "mobs_allowed2",
                    "mob3", "count3", "race3", "size3", "element3", "min_level3", "max_level3", "location3", "mobs_allowed3",
                },
                new object?[] {
                    (uint)id.Value, title, timeLimit,
                    obj[0].Mob, obj[0].Count, obj[0].Race, obj[0].Size, obj[0].Element, obj[0].MinLevel, obj[0].MaxLevel, obj[0].Location, obj[0].MobsAllowed,
                    obj[1].Mob, obj[1].Count, obj[1].Race, obj[1].Size, obj[1].Element, obj[1].MinLevel, obj[1].MaxLevel, obj[1].Location, obj[1].MobsAllowed,
                    obj[2].Mob, obj[2].Count, obj[2].Race, obj[2].Size, obj[2].Element, obj[2].MinLevel, obj[2].MaxLevel, obj[2].Location, obj[2].MobsAllowed,
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
}
