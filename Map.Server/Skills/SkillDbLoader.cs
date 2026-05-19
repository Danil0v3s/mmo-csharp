using Core.Database.Entities;
using Map.Server.Status;

namespace Map.Server.Skills;

/// <summary>
/// Converts a <see cref="SkillDbEntity"/> row (rAthena <c>skill_db</c>
/// SQL table) into a runtime <see cref="SkillDefinition"/>. Mirror of
/// the <c>itemdb_read_sqldb</c> / <c>mob_read_sqldb</c> pattern that
/// MobDb and ItemCatalog already use.
///
/// Per-level columns come as <c>:</c>-delimited strings; missing
/// entries default to 0 / empty.
/// </summary>
public static class SkillDbLoader
{
    public static SkillDefinition FromEntity(SkillDbEntity row)
    {
        var maxLevel = Math.Max((byte)1, row.MaxLevel);
        return new SkillDefinition
        {
            Id = row.Id,
            Name = row.Name,
            MaxLevel = maxLevel,
            Target = ParseTarget(row.TargetMode),
            DamageKind = ParseDamageKind(row.DamageKind),
            Range = row.Range,
            Element = ParseElement(row.Element),
            StatusType = ParseStatus(row.StatusType),
            SpCost = ParsePerLevel(row.SpCost, maxLevel),
            CastTimeMs = ParsePerLevel(row.CastTimeMs, maxLevel),
            CooldownMs = ParsePerLevel(row.CooldownMs, maxLevel),
            DamageRate = ParsePerLevel(row.DamageRate, maxLevel),
            EffectAmount = ParsePerLevel(row.EffectAmount, maxLevel),
            StatusDurationMs = ParsePerLevel(row.StatusDurationMs, maxLevel),
        };
    }

    private static int[] ParsePerLevel(string packed, byte maxLevel)
    {
        var arr = new int[maxLevel + 1]; // index 0 unused; 1..MaxLevel
        if (string.IsNullOrWhiteSpace(packed)) return arr;
        var parts = packed.Split(':', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length && i + 1 <= maxLevel; i++)
        {
            if (int.TryParse(parts[i], out var v)) arr[i + 1] = v;
        }
        return arr;
    }

    private static SkillTargetMode ParseTarget(string mode) => mode switch
    {
        "TargetEnemy" => SkillTargetMode.TargetEnemy,
        "TargetFriend" => SkillTargetMode.TargetFriend,
        "Ground" => SkillTargetMode.Ground,
        "Passive" => SkillTargetMode.Passive,
        _ => SkillTargetMode.SelfOnly,
    };

    private static SkillDamageKind ParseDamageKind(string kind) => kind switch
    {
        "Weapon" => SkillDamageKind.Weapon,
        "Magic" => SkillDamageKind.Magic,
        "Misc" => SkillDamageKind.Misc,
        "Heal" => SkillDamageKind.Heal,
        _ => SkillDamageKind.None,
    };

    private static BattleElement ParseElement(string ele) => ele switch
    {
        "Water" => BattleElement.Water,
        "Earth" => BattleElement.Earth,
        "Fire" => BattleElement.Fire,
        "Wind" => BattleElement.Wind,
        "Poison" => BattleElement.Poison,
        "Holy" => BattleElement.Holy,
        "Dark" => BattleElement.Dark,
        "Ghost" => BattleElement.Ghost,
        "Undead" => BattleElement.Undead,
        _ => BattleElement.Neutral,
    };

    private static StatusType ParseStatus(string sc) => sc switch
    {
        "Blessing" => StatusType.Blessing,
        "IncreaseAgi" => StatusType.IncreaseAgi,
        "DecreaseAgi" => StatusType.DecreaseAgi,
        "Poison" => StatusType.Poison,
        "HealOverTime" => StatusType.HealOverTime,
        _ => StatusType.None,
    };
}
