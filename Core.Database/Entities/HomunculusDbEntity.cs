namespace Core.Database.Entities;

/// <summary>
/// Homunculus class definition (base form + evolution target).
/// Skill tree + per-level stats are separate tables.
/// </summary>
public class HomunculusDbEntity
{
    public string ClassAegis { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FoodItem { get; set; }
    public int? HungryDelay { get; set; }
    public string? Size { get; set; }
    public string? Race { get; set; }
    public string? Element { get; set; }
    public int? EleLevel { get; set; }
    public int? AttackRange { get; set; }
    public string? EvolutionClass { get; set; }
}
