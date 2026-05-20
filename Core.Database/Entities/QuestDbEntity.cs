namespace Core.Database.Entities;

/// <summary>
/// rAthena quest catalog. One row per quest with up to 3 mob-kill
/// objectives flattened inline. Seeded from <c>db/re/quest_db.yml</c>.
/// </summary>
public class QuestDbEntity
{
    public uint QuestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TimeLimit { get; set; } = string.Empty;
    public string? Mob1 { get; set; }
    public int Count1 { get; set; }
    public string? Mob2 { get; set; }
    public int Count2 { get; set; }
    public string? Mob3 { get; set; }
    public int Count3 { get; set; }
}
