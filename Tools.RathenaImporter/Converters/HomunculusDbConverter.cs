using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/homunculus_db.yml</c> → <c>seed_homunculus_db.sql</c>.
/// Subset — class id, name, food item, base stats. Evolution +
/// skill tree go in separate seeds (homun_skill_tree, homun_exp).
/// </summary>
public sealed class HomunculusDbConverter : IYamlToSqlConverter
{
    public string Name => "homunculus_db";
    public string SourceYamlPath => "db/re/homunculus_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_homunculus_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var name = row.Str("Name"); if (name == null) continue;
            sb.AppendLine(SqlEmit.Replace("homunculus_db",
                new[] {
                    "class_aegis", "name", "food_item", "hungry_delay",
                    "size", "race", "element", "ele_level",
                    "attack_range", "evolution_class",
                },
                new object?[] {
                    row.Str("Class") ?? name, name,
                    row.Str("FoodItem"),
                    row.Int("HungryDelay"),
                    row.Str("Size"),
                    row.Str("Race"),
                    row.Str("Element"),
                    row.Int("ElementLevel"),
                    row.Int("AttackRange"),
                    row.Str("EvolutionClass"),
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
