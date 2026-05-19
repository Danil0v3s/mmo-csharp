namespace Core.Database.Entities;

/// <summary>
/// Static skill catalog row — mirror of rAthena's <c>skill_db</c> SQL
/// table (the <c>use_sql_db: yes</c> path of <c>conf/inter_athena.conf</c>).
/// Columns trimmed to what the <c>Map.Server.Skills.SkillDefinition</c>
/// runtime reads today; per-level fields are stored as <c>:</c>-delimited
/// strings (matching the rAthena CSV-style packed schema) and parsed at
/// load time.
///
/// Seeding (rAthena db/re/skill_db.yml has ~1500 rows) is a separate
/// data-migration task. Until that lands, the in-memory
/// <c>SkillDb</c> stays as the catalog source — this entity exists so the
/// repository surface is ready when the seed arrives.
/// </summary>
public class SkillDbEntity
{
    public ushort Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte MaxLevel { get; set; }
    public string TargetMode { get; set; } = "Self";         // SelfOnly / TargetEnemy / TargetFriend / Ground / Passive
    public string DamageKind { get; set; } = "None";          // None / Weapon / Magic / Misc / Heal
    public int Range { get; set; } = 1;
    public string Element { get; set; } = "Neutral";

    /// <summary>Status effect applied by buff/debuff skills. Empty = none.</summary>
    public string StatusType { get; set; } = "None";

    /// <summary>Colon-delimited per-level SP cost ("10:12:14:..."). Empty entries default to 0.</summary>
    public string SpCost { get; set; } = string.Empty;
    public string CastTimeMs { get; set; } = string.Empty;
    public string CooldownMs { get; set; } = string.Empty;
    public string DamageRate { get; set; } = string.Empty;
    public string EffectAmount { get; set; } = string.Empty;
    public string StatusDurationMs { get; set; } = string.Empty;
}
