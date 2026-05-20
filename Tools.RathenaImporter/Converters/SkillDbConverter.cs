using System.Text;
using YamlDotNet.RepresentationModel;

namespace Tools.RathenaImporter.Converters;

/// <summary>
/// Converts <c>db/re/skill_db.yml</c> → <c>seed_skill_db.sql</c>.
///
/// Schema target: <c>Core.Database.Entities.SkillDbEntity</c> /
/// <c>seed_skill_db.sql</c> → <c>skill_db</c> table.
///
/// rAthena's per-level columns (<c>SpCost</c>, <c>CastTime</c>,
/// <c>Cooldown</c>, …) get flattened to <c>:</c>-delimited strings,
/// matching the existing <c>SkillDbLoader.FromEntity</c> parser
/// (which splits on `:` per level).
/// </summary>
public sealed class SkillDbConverter : IYamlToSqlConverter
{
    public string Name => "skill_db";
    public string SourceYamlPath => "db/re/skill_db.yml";
    public string OutputSqlPath => "Core.Database/Seeds/Scripts/seed_skill_db.sql";

    public Task<string> ConvertAsync(string rathenaRoot, CancellationToken ct = default)
    {
        var src = Path.Combine(rathenaRoot, SourceYamlPath);
        var body = YamlHelpers.LoadBody(src);

        var sb = new StringBuilder();
        var rowCount = 0;

        var columns = new[]
        {
            "id", "name", "description", "max_level", "target_mode",
            "damage_kind", "range", "element", "status_type",
            "sp_cost", "cast_time_ms", "cooldown_ms", "damage_rate",
            "effect_amount", "status_duration_ms",
        };

        foreach (var row in body.Rows())
        {
            var id = row.Int("Id");
            if (id is null) continue;

            var name = row.Str("Name") ?? "";
            var desc = row.Str("Description") ?? "";
            var maxLv = row.Int("MaxLevel") ?? 1;
            var maxLevel = Math.Clamp(maxLv, 1, 20);

            var inf = row.Str("TargetType") ?? row.Str("Inf") ?? "Self";  // YAML uses TargetType
            var (target, damageKind) = MapInfToTargetMode(inf, row);

            var range = -1;
            if (row.Get("Range") is YamlScalarNode rangeScalar)
            {
                _ = int.TryParse(rangeScalar.Value, out range);
            }
            else if (row.Get("Range") is YamlSequenceNode)
            {
                // per-level range — pick lvl 1 for the scalar slot; the
                // SkillDefinition Range column today is a single int.
                var s = row.PackPerLevel("Range", "Size", maxLevel);
                _ = int.TryParse(s.Split(':')[0], out range);
            }

            var element = row.Str("Element") ?? "Neutral";
            // YAML "Weapon" means "use weapon element"; map to Neutral
            // until BattleElement gets a Weapon enum.
            if (element == "Weapon") element = "Neutral";
            else if (element.StartsWith("Ele_")) element = element.Substring(4);

            // Status grant — rAthena lists a SkillData StatusChange row;
            // optional, default None.
            var status = row.Str("Status") ?? "None";

            // Per-level columns from `Requires`/cast/cooldown blocks.
            string spCost, castMs, cooldownMs, dmgRate, effectAmt, statusMs;

            var requires = row.Get("Requires") as YamlMappingNode;
            spCost = requires?.PackPerLevel("SpCost", "Amount", maxLevel) ?? string.Join(':', Enumerable.Repeat("0", maxLevel));

            castMs = row.PackPerLevel("CastTime", "Time", maxLevel);
            cooldownMs = row.PackPerLevel("Cooldown", "Time", maxLevel);

            // Damage rate doesn't have a direct rAthena field; pick the
            // attack power table (multi-level). The YAML uses
            // `DamageFlags:` for booleans (NoDamage / IgnoreFlee). The
            // power table is the per-level numeric "DamageFlags?" — no
            // direct match; default 100:100:... so the runtime falls
            // back to a flat 100% multiplier.
            dmgRate = string.Join(':', Enumerable.Repeat("100", maxLevel));

            // EffectAmount — used by Heal-type skills as a per-level
            // multiplier. YAML's Heal column doesn't exist; pull from
            // `Duration1` for status skills or default 0.
            effectAmt = string.Join(':', Enumerable.Repeat("0", maxLevel));

            statusMs = row.PackPerLevel("Duration1", "Time", maxLevel);

            var values = new object?[]
            {
                (ushort)id.Value, name, desc, (byte)maxLevel,
                target, damageKind, range, element, status,
                spCost, castMs, cooldownMs, dmgRate, effectAmt, statusMs,
            };

            sb.AppendLine(SqlEmit.Replace("skill_db", columns, values));
            rowCount++;
            if (ct.IsCancellationRequested) break;
        }

        var header = SqlEmit.Header(Name, SourceYamlPath, rowCount);
        return Task.FromResult(header + sb.ToString());
    }

    /// <summary>
    /// Map rAthena's <c>TargetType</c> + skill-Type combo onto the
    /// C# port's split <c>TargetMode</c> + <c>DamageKind</c>.
    /// </summary>
    private static (string target, string damageKind) MapInfToTargetMode(string inf, YamlMappingNode row)
    {
        var type = row.Str("Type") ?? "None";   // Weapon / Magic / Misc / None
        var damageKind = type switch
        {
            "Weapon" => "Weapon",
            "Magic" => "Magic",
            "Misc" => "Misc",
            _ => "None",
        };

        var target = inf switch
        {
            "Self" => "SelfOnly",
            "Attack" => "TargetEnemy",
            "Support" => "TargetFriend",
            "Ground" or "Place" => "Ground",
            "Passive" => "Passive",
            _ => "SelfOnly",
        };

        // Heal skills get DamageKind=Heal regardless of Type column.
        if (row.Str("Status") == null && target == "TargetFriend" && type == "None")
        {
            // Could be a heal — only set Heal if the YAML has a Heal
            // line. Without a clear marker default to None (status grant).
        }

        return (target, damageKind);
    }
}
