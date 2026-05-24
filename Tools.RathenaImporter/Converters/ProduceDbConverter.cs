using System.Text;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// <c>db/re/produce_db.txt</c> (rAthena's pre-YAML CSV format) →
/// <c>seed_produce_recipe_db.sql</c>. Parent table:
/// <c>(id, produce_item_id, item_lv, require_skill_id, require_skill_lv)</c>.
/// Materials emitted as separate INSERTs into
/// <c>produce_recipe_material_db</c> with composite key
/// <c>(recipe_id, slot_index)</c>.
///
/// <para>Format: <c>id, produce_item_id, item_lv, require_skill,
/// require_skill_lv, mat_id_1, mat_amt_1, mat_id_2, mat_amt_2, …</c>
/// — variable-length material list. <c>mat_amt == 0</c> denotes a
/// must-own-but-do-not-consume guide item.</para>
/// </summary>
public sealed class ProduceDbConverter : IYamlToSqlConverter
{
    public string Name => "produce_db";
    public string SourceYamlPath => "db/re/produce_db.txt";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_produce_recipe_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var path = Path.Combine(rathenaRoot, SourceYamlPath);
        var recipes = 0;
        var materials = 0;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            // Need at least id, produce_item_id, item_lv, req_skill, req_skill_lv.
            if (parts.Length < 5) continue;
            if (!int.TryParse(parts[0], out var id)) continue;
            if (!uint.TryParse(parts[1], out var produceItemId)) continue;
            if (!byte.TryParse(parts[2], out var itemLv)) continue;
            if (!ushort.TryParse(parts[3], out var reqSkill)) continue;
            if (!byte.TryParse(parts[4], out var reqSkillLv)) continue;
            if (produceItemId == 0) continue; // rAthena treats 0 produce_item as "remove this row".
            sb.AppendLine(SqlEmit.Replace("produce_recipe_db",
                new[] { "id", "produce_item_id", "item_lv", "require_skill_id", "require_skill_lv" },
                new object?[] { id, produceItemId, itemLv, reqSkill, reqSkillLv }));
            recipes++;
            byte slot = 0;
            for (var i = 5; i + 1 < parts.Length; i += 2)
            {
                if (!uint.TryParse(parts[i], out var matId)) continue;
                if (!uint.TryParse(parts[i + 1], out var matAmt)) continue;
                if (matId == 0) continue;
                sb.AppendLine(SqlEmit.Replace("produce_recipe_material_db",
                    new[] { "recipe_id", "slot_index", "material_item_id", "material_amount" },
                    new object?[] { id, slot, matId, matAmt }));
                slot++;
                materials++;
            }
        }
        return Task.FromResult(
            SqlEmit.Header(Name, SourceYamlPath, recipes) +
            $"-- Materials: {materials}{Environment.NewLine}" +
            sb);
    }
}
