namespace Core.Database.Entities;

/// <summary>
/// Elemental attack vs defence multiplier matrix
/// (rAthena <c>db/re/attr_fix.yml</c>). One row per
/// (element-level, attacker-element, defender-element) tuple gives
/// the percentage damage multiplier the calculator applies.
///
/// Example: Level=2, AttackerElement=Fire, DefenderElement=Water →
/// Multiplier=50 means a level-2 Fire weapon deals 50% damage to
/// a Water-element defender.
///
/// Composite key (Level, AttackerElement, DefenderElement). DB-8a
/// wave re-normalized from the prior <c>PayloadIntKeyEntity</c>
/// JSON-blob layout (eyeball-decoding a 10×10 matrix from JSON each
/// load was the worst of the payload patterns).
/// </summary>
public class AttrFixDbEntity
{
    /// <summary>
    /// Element refinement level (1..4). Determines which sub-matrix
    /// applies — higher-level weapons have stronger effective bonuses
    /// against same-tier defenders.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Attacker's weapon element (string Aegis name: Neutral, Water,
    /// Earth, Fire, Wind, Poison, Holy, Dark, Ghost, Undead).
    /// </summary>
    public string AttackerElement { get; set; } = string.Empty;

    /// <summary>Defender's defence element (same Aegis names).</summary>
    public string DefenderElement { get; set; } = string.Empty;

    /// <summary>
    /// Damage multiplier in percent (range -100..200; 100 = neutral,
    /// 0 = immune, 200 = 2× weak point).
    /// </summary>
    public int Multiplier { get; set; }
}
