namespace Core.Database.Entities;

/// <summary>
/// Achievement-level XP curve row (rAthena
/// <c>db/re/achievement_level_db.yml</c>). Defines how many achievement
/// points are required to reach each Achievement Level (1..20 by
/// default). The achievement service awards level-ups as the player
/// crosses each <see cref="RequiredPoints"/> threshold.
///
/// AT-G wave added this entity — DB-1..6 ported achievement_db (the
/// per-achievement catalog) but skipped the level-curve table.
/// </summary>
public class AchievementLevelDbEntity
{
    /// <summary>Achievement level (1-based; rAthena caps at 20 in stock yml).</summary>
    public int Level { get; set; }

    /// <summary>
    /// Cumulative points required to reach this level. The next-level
    /// threshold is the next-higher row's value; current-row value =
    /// "points needed since previous level."
    /// </summary>
    public long RequiredPoints { get; set; }
}
