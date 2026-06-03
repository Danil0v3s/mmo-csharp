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
            // COMBAT-92 — Requirements / Flags / Unit columns (replaces the curated overlays).
            AmmoTypeMask = ParseAmmoMask(row.Ammo),
            AmmoQuantity = BroadcastAmmoQty(row.AmmoAmount, maxLevel),
            Inf2 = ParseFlags<SkillInf2>(row.Inf2),
            UnitFlags = ParseFlags<SkillUnitFlag>(row.UnitFlags),
        };
    }

    // COMBAT-92 — rAthena e_ammo_type bits (pc.hpp): require.ammo is `1<<AMMO_x`.
    // Mirrors the SkillDb curated AmmoArrow..AmmoThrow constants this loader replaces.
    private static readonly IReadOnlyDictionary<string, int> AmmoNameBits =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arrow"] = 1 << 1, ["Dagger"] = 1 << 2, ["Bullet"] = 1 << 3, ["Shell"] = 1 << 4,
            ["Grenade"] = 1 << 5, ["Shuriken"] = 1 << 6, ["Kunai"] = 1 << 7, ["Cannonball"] = 1 << 8,
            ["Throwweapon"] = 1 << 9,
        };

    /// <summary>Pipe-delimited ammo-type names → OR'd `1&lt;&lt;AMMO_x` mask. Unknown tokens skipped.</summary>
    private static int ParseAmmoMask(string ammo)
    {
        if (string.IsNullOrWhiteSpace(ammo)) return 0;
        var mask = 0;
        foreach (var tok in ammo.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (AmmoNameBits.TryGetValue(tok, out var bit)) mask |= bit;
        return mask;
    }

    /// <summary>Renewal skill_db uses one ammo amount for every level — broadcast it (1-indexed).</summary>
    private static int[] BroadcastAmmoQty(int amount, byte maxLevel)
    {
        var arr = new int[maxLevel + 1];
        if (amount <= 0) return arr;
        for (var lv = 1; lv <= maxLevel; lv++) arr[lv] = amount;
        return arr;
    }

    /// <summary>Pipe-delimited flag names → OR'd flags enum. Tokens that don't name a known member
    /// of <typeparamref name="TFlag"/> are silently skipped (the runtime only models a subset).</summary>
    private static TFlag ParseFlags<TFlag>(string flags) where TFlag : struct, Enum
    {
        ulong acc = 0;
        if (!string.IsNullOrWhiteSpace(flags))
            foreach (var tok in flags.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (Enum.TryParse<TFlag>(tok, ignoreCase: true, out var f))
                    acc |= Convert.ToUInt64(f);
        return (TFlag)Enum.ToObject(typeof(TFlag), acc);
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
        // COMBAT-19 — skill-element sentinels resolved per cast at runtime.
        "All" => BattleElement.All,
        "Weapon" => BattleElement.Weapon,
        "Endowed" => BattleElement.Endowed,
        "Random" => BattleElement.Random,
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
