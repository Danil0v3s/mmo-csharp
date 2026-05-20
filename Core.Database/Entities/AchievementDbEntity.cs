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
}
