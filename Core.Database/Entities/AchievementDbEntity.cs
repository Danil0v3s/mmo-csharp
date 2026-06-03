namespace Core.Database.Entities;

/// <summary>
/// Achievement catalog. Seeded from <c>db/re/achievement_db.yml</c>.
/// Targets flattened to "mob:count;mob:count" — runtime parses
/// back into per-objective rows.
/// </summary>
public class AchievementDbEntity
{
    public uint AchievementId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Dependents { get; set; } = string.Empty;
    public string Targets { get; set; } = string.Empty;

    // FEATURE-04 — completion reward (the first Rewards[] entry; rAthena grants on manual claim).
    /// <summary>Reward item aegis name (empty = no item reward). Resolved to an id at grant time.</summary>
    public string RewardItem { get; set; } = string.Empty;
    /// <summary>Reward item amount (default 1 when an item is set).</summary>
    public int RewardAmount { get; set; }
    /// <summary>Reward title id granted on claim (0 = none).</summary>
    public int RewardTitleId { get; set; }
}
