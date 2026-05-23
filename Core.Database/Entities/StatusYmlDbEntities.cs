namespace Core.Database.Entities;

/// <summary>
/// Per-SC (status change) catalog entry from rAthena
/// <c>db/re/status.yml</c>. One row per Status name with scalar
/// metadata. The 7 nested boolean maps (States / CalcFlags / Flags /
/// Fail / EndOnStart / EndOnEnd / EndOnRestart) collapse into a single
/// child table <see cref="StatusDbFlagEntity"/> with a category
/// discriminator — cleaner than 7 separate tables since most queries
/// are "does SC X have flag Y in category Z?" which is one WHERE.
///
/// DB-8e wave replaces the prior <c>StatusYmlEntity :
/// PayloadStringKeyEntity</c> JSON blob (the largest payload table:
/// ~440 SCs each with deep nested flag maps).
/// </summary>
public class StatusDbEntity
{
    /// <summary>SC name (e.g. "Stone", "Freeze", "Blessing").</summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// rAthena <c>DurationLookup</c> — skill Aegis name whose Duration1
    /// is the canonical timer for this SC (e.g. "NPC_PETRIFYATTACK").
    /// Null when SC has hardcoded duration.
    /// </summary>
    public string? DurationLookup { get; set; }

    /// <summary>rAthena <c>Opt1</c> — visible status icon set 1 (e.g. "Stone", "Freeze").</summary>
    public string? Opt1 { get; set; }

    /// <summary>rAthena <c>Opt2</c> — visible status icon set 2.</summary>
    public string? Opt2 { get; set; }

    /// <summary>rAthena <c>Opt3</c> — visible status icon set 3.</summary>
    public string? Opt3 { get; set; }
}

/// <summary>
/// One flag in one of the SC's nested boolean maps. Composite key
/// (StatusName, Category, FlagName).
///
/// Category enum values:
/// - "State"        — sd-&gt;sc.cant.* flags (NoMove, NoCast, NoAttack…)
/// - "CalcFlag"     — status_calc_pc recalc triggers (Def_Ele, Def, Mdef…)
/// - "Flag"         — generic SC flags (SendOption, BossResist…)
/// - "Fail"         — SCs that, if active on target, block this SC
/// - "EndOnStart"   — SCs to end when this one starts
/// - "EndOnEnd"     — SCs to end when this one ends
/// - "EndOnRestart" — SCs to end when this one is refreshed/restarted
/// </summary>
public class StatusDbFlagEntity
{
    /// <summary>FK to <see cref="StatusDbEntity.StatusName"/>.</summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>One of: State, CalcFlag, Flag, Fail, EndOnStart, EndOnEnd, EndOnRestart.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Flag name within the category. For Fail/EndOn* this is another
    /// SC name; for State/CalcFlag/Flag it's a flag enum value
    /// (NoMove, Def_Ele, BossResist, etc.).
    /// </summary>
    public string FlagName { get; set; } = string.Empty;
}
