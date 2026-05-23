namespace Core.Database.Entities;

/// <summary>
/// Random mob summon group (rAthena <c>db/mob_summon.yml</c>). One
/// row per group name (BLOODY_DEAD_BRANCH, POSITIVE2, REGULAR, etc.).
/// Used by branch items (@summon items) and certain script commands
/// to pick a random mob from a weighted list.
///
/// Members live in <see cref="MobSummonEntryDbEntity"/>. DB-8b wave
/// replaces the prior <c>PayloadStringKeyEntity</c> JSON-blob layout.
/// </summary>
public class MobSummonDbEntity
{
    /// <summary>Group key (e.g. "BLOODY_DEAD_BRANCH").</summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Default mob Aegis name picked when the random roll fails the
    /// rate gates (rAthena fall-through default).
    /// </summary>
    public string DefaultMobAegis { get; set; } = string.Empty;
}

/// <summary>
/// One weighted member of a <see cref="MobSummonDbEntity"/>. Composite
/// key (GroupName, MobAegis). The runtime picks a mob by rolling
/// against the per-row Rate (typically out of 1_000_000).
/// </summary>
public class MobSummonEntryDbEntity
{
    /// <summary>FK to <see cref="MobSummonDbEntity.GroupName"/>.</summary>
    public string GroupName { get; set; } = string.Empty;
    /// <summary>Member mob Aegis name (FK into mob_db).</summary>
    public string MobAegis { get; set; } = string.Empty;
    /// <summary>Weight / 1_000_000. Sum of rates per group ≈ 1_000_000.</summary>
    public int Rate { get; set; }
}
