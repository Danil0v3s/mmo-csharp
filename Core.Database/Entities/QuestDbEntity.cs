namespace Core.Database.Entities;

/// <summary>
/// rAthena quest catalog. One row per quest with up to 3 kill objectives flattened inline. Each
/// objective is either mob-specific (<c>MobN</c> set) or an "any-mob" filter (<c>MobN</c> empty,
/// matched by <c>RaceN</c>/<c>SizeN</c>/<c>ElementN</c>/<c>MinLevelN</c>/<c>MaxLevelN</c>/<c>LocationN</c>
/// and an optional <c>MobsAllowedN</c> allow-list). Mirrors <c>s_quest_db::objectives</c>
/// (quest.cpp). Seeded from <c>db/re/quest_db.yml</c>.
/// </summary>
public class QuestDbEntity
{
    public uint QuestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TimeLimit { get; set; } = string.Empty;

    public string? Mob1 { get; set; }
    public int Count1 { get; set; }
    public string? Race1 { get; set; }
    public string? Size1 { get; set; }
    public string? Element1 { get; set; }
    public int MinLevel1 { get; set; }
    public int MaxLevel1 { get; set; }
    public string? Location1 { get; set; }
    public string? MobsAllowed1 { get; set; }

    public string? Mob2 { get; set; }
    public int Count2 { get; set; }
    public string? Race2 { get; set; }
    public string? Size2 { get; set; }
    public string? Element2 { get; set; }
    public int MinLevel2 { get; set; }
    public int MaxLevel2 { get; set; }
    public string? Location2 { get; set; }
    public string? MobsAllowed2 { get; set; }

    public string? Mob3 { get; set; }
    public int Count3 { get; set; }
    public string? Race3 { get; set; }
    public string? Size3 { get; set; }
    public string? Element3 { get; set; }
    public int MinLevel3 { get; set; }
    public int MaxLevel3 { get; set; }
    public string? Location3 { get; set; }
    public string? MobsAllowed3 { get; set; }
}
