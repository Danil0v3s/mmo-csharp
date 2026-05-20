using System.Text;
using YamlDotNet.RepresentationModel;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/pet_db.yml</c> → <c>seed_pet_db.sql</c>.
/// Pet definitions — bonded mob + tame item + egg + food + intimacy
/// table + per-pet bonus script.
/// </summary>
public sealed class PetDbConverter : IYamlToSqlConverter
{
    public string Name => "pet_db";
    public string SourceYamlPath => "db/re/pet_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_pet_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var mob = row.Str("Mob"); if (mob == null) continue;
            sb.AppendLine(SqlEmit.Replace("pet_db",
                new[] {
                    "mob_aegis", "tame_item", "egg_item", "equip_item", "food_item",
                    "fullness", "hunger_delay", "intimacy_start", "intimacy_fed",
                    "intimacy_overfed", "intimacy_hungry", "intimacy_owner_die",
                    "capture_rate", "special_performance", "attack_rate",
                    "retaliate_rate", "change_target_rate", "allow_auto_feed",
                    "script", "support_script",
                },
                new object?[] {
                    mob,
                    row.Str("TameItem"),
                    row.Str("EggItem"),
                    row.Str("EquipItem"),
                    row.Str("FoodItem"),
                    row.Int("Fullness"),
                    row.Int("HungerDelay"),
                    row.Int("IntimacyStart"),
                    row.Int("IntimacyFed"),
                    row.Int("IntimacyOverfed"),
                    row.Int("IntimacyHungry"),
                    row.Int("IntimacyOwnerDie"),
                    row.Int("CaptureRate"),
                    row.Bool("SpecialPerformance"),
                    row.Int("AttackRate"),
                    row.Int("RetaliateRate"),
                    row.Int("ChangeTargetRate"),
                    row.Bool("AllowAutoFeed"),
                    (row.Get("Script") as YamlScalarNode)?.Value,
                    (row.Get("SupportScript") as YamlScalarNode)?.Value,
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
