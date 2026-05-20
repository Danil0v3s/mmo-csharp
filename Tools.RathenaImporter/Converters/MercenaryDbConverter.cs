using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary><c>db/re/mercenary_db.yml</c> → <c>seed_mercenary_db.sql</c>.</summary>
public sealed class MercenaryDbConverter : IYamlToSqlConverter
{
    public string Name => "mercenary_db";
    public string SourceYamlPath => "db/re/mercenary_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_mercenary_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var body = YamlHelpers.LoadBody(Path.Combine(rathenaRoot, SourceYamlPath));
        var sb = new StringBuilder();
        var n = 0;
        foreach (var row in body.Rows())
        {
            var id = row.Int("Id"); if (id == null) continue;
            sb.AppendLine(SqlEmit.Replace("mercenary_db",
                new[] {
                    "merc_id", "aegis_name", "name", "level",
                    "hp", "sp", "attack", "attack2", "defense", "magic_defense",
                    "str", "agi", "vit", "intel", "dex", "luk",
                    "attack_range", "skill_range", "chase_range",
                    "size", "race", "element", "ele_level",
                    "walk_speed", "attack_delay", "attack_motion", "damage_motion",
                },
                new object?[] {
                    (uint)id.Value,
                    row.Str("AegisName"),
                    row.Str("Name"),
                    row.Int("Level"),
                    row.Int("Hp"), row.Int("Sp"),
                    row.Int("Attack"), row.Int("Attack2"),
                    row.Int("Defense"), row.Int("MagicDefense"),
                    row.Int("Str"), row.Int("Agi"), row.Int("Vit"),
                    row.Int("Int"), row.Int("Dex"), row.Int("Luk"),
                    row.Int("AttackRange"), row.Int("SkillRange"), row.Int("ChaseRange"),
                    row.Str("Size"), row.Str("Race"), row.Str("Element"), row.Int("ElementLevel"),
                    row.Int("WalkSpeed"), row.Int("AttackDelay"),
                    row.Int("AttackMotion"), row.Int("DamageMotion"),
                }));
            n++;
        }
        return Task.FromResult(SqlEmit.Header(Name, SourceYamlPath, n) + sb);
    }
}
