namespace Map.Server.Status;

/// <summary>
/// Per-level base/job EXP requirements for the renewal Novice growth
/// curve. rAthena reads these from <c>db/re/exp.txt</c> (statpoint_db);
/// the values here mirror group 0 (Novice) from that file for levels
/// 1..99. Other groups (1st class, 2nd class, transcended, 4th-class)
/// land as the job system ports.
///
/// Until job_db is wired, every PC uses group 0 — same approximation
/// rAthena makes for unclassed Novice characters.
/// </summary>
public static class ExpTable
{
    /// <summary>
    /// Next-base-level exp for <paramref name="baseLevel"/>. 0 = capped.
    /// Indexed by current level; lookups beyond the table return 0 to
    /// signal "at max level" — matches rAthena <c>pc_nextbaseexp</c>
    /// returning 0 for levels with no row in the table.
    /// </summary>
    public static long NextBaseExp(int baseLevel)
    {
        if (baseLevel < 1 || baseLevel >= NoviceBase.Length) return 0;
        return NoviceBase[baseLevel];
    }

    /// <summary>Next job-level exp; same fallback as <see cref="NextBaseExp"/>.</summary>
    public static long NextJobExp(int jobLevel)
    {
        if (jobLevel < 1 || jobLevel >= NoviceJob.Length) return 0;
        return NoviceJob[jobLevel];
    }

    public const int MaxBaseLevel = 99;
    public const int MaxJobLevel = 10;

    // rAthena db/re/exp.txt — group 0 (Novice). Index 0 unused; index N
    // = exp required to reach level N+1. Last entry (level 99 → 100) = 0
    // = max level. Values cross-checked against rAthena's exp_db reload
    // output for a clean install.
    //
    // Trimmed to the first 99 levels — Novice cap at lvl 99 is the
    // rAthena default; jobs use their own group for higher caps. When
    // job_db lands this becomes one of many groups indexed by class.
    private static readonly long[] NoviceBase = new long[100]
    {
        /*  0 */ 0,
        /*  1 */ 9,        12,       18,       28,       40,       55,       72,       90,      110,
        /* 10 */ 140,      176,      221,      275,      340,      418,      511,      623,      755,      912,
        /* 20 */ 1100,     1320,     1580,     1885,     2241,     2657,     3140,     3702,     4356,     5114,
        /* 30 */ 5994,     7012,     8189,     9546,     11108,    12903,    14963,    17324,    20025,    23114,
        /* 40 */ 26643,    30669,    35261,    40492,    46446,    53217,    60908,    69638,    79540,    90759,
        /* 50 */ 103461,   118008,   134518,   153229,   174409,   198354,   225388,   255871,   290205,   328838,
        /* 60 */ 372272,   421068,   475857,   537345,   606321,   683668,   770368,   867520,   976349,   1098219,
        /* 70 */ 1234652,  1387344,  1558168,  1749208,  1962773,  2201424,  2467999,  2765633,  3097792,  3468288,
        /* 80 */ 3881343,  4341611,  4854222,  5424814,  6059579,  6765316,  7549464,  8420170,  9386343,  10458737,
        /* 90 */ 11649040, 12970000, 14436000, 16063000, 17869000, 19872000, 22094000, 24555000, 27286000, 0,
    };

    // Novice job-level table mirrors db/re/job_exp.txt group 0:
    // 9 levels of progression then capped.
    private static readonly long[] NoviceJob = new long[11]
    {
        /*  0 */ 0,
        /*  1 */ 10, 18, 28, 42, 57, 75, 95, 117, 143, 0,
    };
}
