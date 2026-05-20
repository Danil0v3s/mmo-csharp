using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/spellbook_db.yml</c> → <c>seed_spellbook_db.sql</c>.
/// Maps Warlock spell-book items to the spell they grant.
/// </summary>
public sealed class SpellbookDbConverter : IYamlToSqlConverter
{
    public string Name => "spellbook_db";
    public string SourceYamlPath => "db/re/spellbook_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_spellbook_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var skill = row.Str("Skill");
            var book = row.Str("Book");
            var pp = row.Int("PreservePoints") ?? 0;
            if (skill == null || book == null) continue;
            sb.AppendLine(SqlEmit.Replace("spellbook_db",
                new[] { "skill_name", "book_name_aegis", "preserve_points" },
                new object?[] { skill, book, pp }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
