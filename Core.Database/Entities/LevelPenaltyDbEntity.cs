namespace Core.Database.Entities;

/// <summary>
/// Level-gap penalty curve (rAthena <c>db/re/level_penalty.yml</c>).
/// Defines per-penalty-type buckets of level-difference → rate
/// modifier (applied to EXP / drop / MVP-EXP / MVP-drop when a
/// player's base level differs from the mob's by N levels).
///
/// Parent row; child rows live in
/// <see cref="LevelPenaltyDifferenceDbEntity"/>. DB-8a wave
/// re-normalized from the prior <c>PayloadStringKeyEntity</c>
/// JSON-blob layout that DB-5 punted to a single payload_json column.
/// </summary>
public class LevelPenaltyDbEntity
{
    /// <summary>
    /// Penalty type — one of "Exp", "Drop", "Mvp_Exp", "Mvp_Drop"
    /// (mirrors rAthena <c>enum e_penalty_type</c>). The differences
    /// list under <see cref="LevelPenaltyDifferenceDbEntity"/> hangs
    /// off this key.
    /// </summary>
    public string PenaltyType { get; set; } = string.Empty;
}

/// <summary>
/// One level-difference → rate-modifier row in the
/// <see cref="LevelPenaltyDbEntity"/> curve. Composite key on
/// (PenaltyType, Difference). The runtime evaluator walks rows ordered
/// by ascending difference and returns the rate at the largest delta
/// ≤ |player_lvl − mob_lvl|.
/// </summary>
public class LevelPenaltyDifferenceDbEntity
{
    /// <summary>FK to <see cref="LevelPenaltyDbEntity.PenaltyType"/>.</summary>
    public string PenaltyType { get; set; } = string.Empty;

    /// <summary>
    /// Signed level difference (player − mob). Positive = player
    /// outlevels mob; negative = under-levelled. The rAthena yml
    /// bucket boundaries are non-uniform (e.g. ranges 3..16 are
    /// per-1, then jumps to -6, -11, -16, -21, -26, -31).
    /// </summary>
    public int Difference { get; set; }

    /// <summary>
    /// Rate applied to original EXP/drop rate (basis-points: 0..10000;
    /// 100 = 1× / unchanged, 40 = 0.4×, 140 = 1.4×).
    /// </summary>
    public int Rate { get; set; }
}
