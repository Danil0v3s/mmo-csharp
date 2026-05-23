namespace Core.Database.Entities;

/// <summary>
/// Per-job stat tier (rAthena <c>db/re/job_stats.yml</c>). One row
/// per job class with the base derived-stat constants (max-weight +
/// HP/SP curves). The rAthena yml groups multiple jobs under a single
/// shared <c>Jobs:</c> map; we denormalize per-job so a SQL query
/// can resolve "what's Knight's max weight?" in one statement.
///
/// DB-8d wave replaces the prior <c>JobStatsEntity :
/// PayloadRowIndexEntity</c> JSON blob.
/// </summary>
public class JobInfoDbEntity
{
    /// <summary>Job Aegis name (e.g. "Novice", "Knight", "Rune_Knight").</summary>
    public string JobAegis { get; set; } = string.Empty;

    /// <summary>rAthena <c>MaxWeight</c> — base carry weight (default 20000).</summary>
    public int MaxWeight { get; set; } = 20000;

    /// <summary>
    /// rAthena <c>HpFactor</c> — exponential HP increase per base level.
    /// Used when HP_SP_TABLES macro is disabled. Default 0 = use table.
    /// </summary>
    public int HpFactor { get; set; }

    /// <summary>rAthena <c>HpIncrease</c> — linear HP increase / 100 per base level.</summary>
    public int HpIncrease { get; set; } = 500;

    /// <summary>rAthena <c>SpFactor</c> — exponential SP increase.</summary>
    public int SpFactor { get; set; }

    /// <summary>rAthena <c>SpIncrease</c> — linear SP increase / 100 per base level.</summary>
    public int SpIncrease { get; set; } = 100;
}

/// <summary>
/// Per-job per-level stat bonus row from
/// <c>db/re/job_stats.yml</c>'s <c>BonusStats[]</c>. Composite key
/// (JobAegis, Level). Only the stats that change at this level have
/// non-zero values; rAthena yml omits zeros.
/// </summary>
public class JobBonusStatsDbEntity
{
    public string JobAegis { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Str { get; set; }
    public int Agi { get; set; }
    public int Vit { get; set; }
    public int IntStat { get; set; }
    public int Dex { get; set; }
    public int Luk { get; set; }
    // Renewal trait stats (Power / Stamina / Wisdom / Spell / Concentration / Creative).
    public int Pow { get; set; }
    public int Sta { get; set; }
    public int Wis { get; set; }
    public int Spl { get; set; }
    public int Con { get; set; }
    public int Crt { get; set; }
}

/// <summary>
/// Per-job per-level EXP curve (rAthena <c>db/re/job_exp.yml</c>).
/// One row per (JobAegis, Level). Both <see cref="BaseExp"/> and
/// <see cref="JobExp"/> can be present (rAthena rows may carry one
/// or both, depending on the class's job-level cap).
///
/// DB-8d wave replaces the prior <c>JobExpEntity :
/// PayloadRowIndexEntity</c> JSON blob.
/// </summary>
public class JobExpDbEntity
{
    public string JobAegis { get; set; } = string.Empty;
    public int Level { get; set; }
    /// <summary>Cumulative base-level EXP requirement (null if row only carries JobExp).</summary>
    public long? BaseExp { get; set; }
    /// <summary>Cumulative job-level EXP requirement (null if row only carries BaseExp).</summary>
    public long? JobExp { get; set; }
}

/// <summary>
/// Max-level per class (rAthena <c>job_exp.yml</c>'s
/// <c>MaxBaseLevel</c> + <c>MaxJobLevel</c>). One row per JobAegis.
/// Split off from <see cref="JobExpDbEntity"/> so the caps are a
/// single-row lookup instead of "scan to end."
/// </summary>
public class JobMaxLevelDbEntity
{
    public string JobAegis { get; set; } = string.Empty;
    public int? MaxBaseLevel { get; set; }
    public int? MaxJobLevel { get; set; }
}

/// <summary>
/// Per-job per-level HP/SP/AP base values (rAthena
/// <c>db/re/job_basepoints.yml</c>). One row per (JobAegis, Level).
/// Used by the status calc when HP_SP_TABLES macro is enabled — the
/// HpFactor/SpFactor curves are bypassed and this lookup wins.
///
/// DB-8d wave replaces the prior <c>JobBasePointsEntity :
/// PayloadRowIndexEntity</c> JSON blob.
/// </summary>
public class JobBasePointsDbEntity
{
    public string JobAegis { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Hp { get; set; }
    public int Sp { get; set; }
    /// <summary>rAthena <c>BaseAp[]</c> — 4th-class astral points (null pre-renewal).</summary>
    public int? Ap { get; set; }
}
