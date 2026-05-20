namespace Core.Database.Entities;

/// <summary>
/// Abracadabra random-skill pool. Each row is one skill the
/// SA_ABRACADABRA roll can pick. Seeded from
/// <c>db/abra_db.yml</c> via Tools.RathenaImporter.
/// </summary>
public class AbraDbEntity
{
    public string SkillName { get; set; } = string.Empty;
}
