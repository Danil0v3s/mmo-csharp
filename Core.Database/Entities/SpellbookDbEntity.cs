namespace Core.Database.Entities;

/// <summary>
/// Warlock Spell Book — maps a book item (Aegis name) to the spell
/// it grants and the preserve points needed.
/// </summary>
public class SpellbookDbEntity
{
    public string SkillName { get; set; } = string.Empty;
    public string BookNameAegis { get; set; } = string.Empty;
    public int PreservePoints { get; set; }
}
