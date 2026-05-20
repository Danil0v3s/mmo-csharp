namespace Core.Database.Entities;

/// <summary>
/// Magic Mushroom random-skill pool. SC_MAGICMUSHROOM ticks roll one
/// row. Seeded from <c>db/re/magicmushroom_db.yml</c>.
/// </summary>
public class MagicMushroomDbEntity
{
    public string SkillName { get; set; } = string.Empty;
}
