using Map.Server.Status;

namespace Map.Server.Inventory;

/// <summary>
/// Accumulated runtime bonus numbers from the attacker's equipment.
/// Mirrors rAthena's <c>indexed_bonus</c> struct on
/// <c>map_session_data</c> (status.hpp:1980). One bundle per PC,
/// recomputed by <see cref="EquipBonusAggregator"/> whenever the
/// equipment changes (equip / unequip / break / strip / refine).
///
/// <para><b>Convention:</b> Every percent column is stored as the
/// percent value itself (e.g. <c>+20 % vs Demi-Human</c> ⇒ array
/// slot 20). <c>BattleCardService.CalcCardFix</c> divides by 100
/// when applying. Negative values are valid — Holy Cross armors
/// reduce damage taken from Undead, etc.</para>
///
/// <para><b>Item-script coverage:</b> the
/// <see cref="BonusScriptExtractor"/> regex pass populates this
/// bundle from each equipped item's <c>script</c> column in
/// <c>item_db</c>. It covers the most common <c>bonus</c> /
/// <c>bonus2</c> / <c>bonus3</c> / <c>bonus4</c> patterns; the
/// long tail (conditional `if`, computed `getrefine() * N`,
/// auto-cast, drain rolls) lands when the script engine ports.
/// Each pattern lands as a row in this bundle so future ports
/// flow through without touching the call sites.</para>
/// </summary>
public sealed class EquipBonusBundle
{
    // Per-race indexed bonuses (use (int)BattleRace as index, size
    // = BattleRace.Max). Renewal "All" slot accumulates the
    // race-agnostic bonus (bonus2 bAddRace,RC_All,20 → AddRace[All]).
    private const int RaceSize = (int)BattleRace.Max;
    private const int ElementSize = (int)BattleElement.Max;
    private const int SizeArrSize = (int)BattleSize.All + 1;
    private const int ClassSize = 4; // Normal, Boss, Guardian, All

    public int[] AddRace  { get; } = new int[RaceSize];
    public int[] SubRace  { get; } = new int[RaceSize];
    public int[] AddEle   { get; } = new int[ElementSize];
    public int[] SubEle   { get; } = new int[ElementSize];
    public int[] AddSize  { get; } = new int[SizeArrSize];
    public int[] SubSize  { get; } = new int[SizeArrSize];
    public int[] AddClass { get; } = new int[ClassSize];
    public int[] SubClass { get; } = new int[ClassSize];

    /// <summary>Catch-all flat-ATK additive bonus (<c>bonus bAtk, N</c>).</summary>
    public int FlatAtk { get; set; }
    /// <summary>Catch-all flat-MATK additive bonus (<c>bonus bMatk, N</c>).</summary>
    public int FlatMatk { get; set; }
    /// <summary>Flat extra crit (<c>bonus bCritical, N</c>); displayed in tenths client-side but here stored ×1.</summary>
    public int FlatCritical { get; set; }
    /// <summary>Flat hit bonus (<c>bonus bHit, N</c>).</summary>
    public int FlatHit { get; set; }
    /// <summary>Flat flee bonus (<c>bonus bFlee, N</c>).</summary>
    public int FlatFlee { get; set; }
    /// <summary>Flat aspd bonus (<c>bonus bAspd, N</c>) — rate, not absolute.</summary>
    public int FlatAspd { get; set; }
    /// <summary>Flat aspd percent (<c>bonus bAspdRate, N</c>).</summary>
    public int FlatAspdRate { get; set; }
    /// <summary>Flat MaxHP bonus.</summary>
    public int FlatMaxHp { get; set; }
    /// <summary>Flat MaxSP bonus.</summary>
    public int FlatMaxSp { get; set; }
    /// <summary>Percent MaxHP bonus (additive).</summary>
    public int MaxHpRate { get; set; }
    /// <summary>Percent MaxSP bonus (additive).</summary>
    public int MaxSpRate { get; set; }
    /// <summary>Long-range damage percent.</summary>
    public int LongAtkRate { get; set; }
    /// <summary>Short-range (melee) damage percent.</summary>
    public int ShortAtkRate { get; set; }
    /// <summary>Critical damage % bonus (<c>bonus bCriticalAddRace</c> sums separately).</summary>
    public int CritAtkRate { get; set; }

    // Cast-time / delay knobs consumed by SkillCastTimingService.
    public int VarCastRate { get; set; }     // %
    public int FixCastRate { get; set; }     // %
    public int AddVarCastMs { get; set; }    // ms
    public int AddFixCastMs { get; set; }    // ms
    public int DelayRate { get; set; }       // %

    // Drain (renewal: hp/sp drained per hit).
    public int DrainHpRate { get; set; }     // % chance × 100
    public int DrainSpRate { get; set; }     // % chance × 100

    /// <summary>Reset all fields. Cheap allocation-free recycle.</summary>
    public void Reset()
    {
        Array.Clear(AddRace); Array.Clear(SubRace);
        Array.Clear(AddEle); Array.Clear(SubEle);
        Array.Clear(AddSize); Array.Clear(SubSize);
        Array.Clear(AddClass); Array.Clear(SubClass);
        FlatAtk = FlatMatk = FlatCritical = FlatHit = FlatFlee = 0;
        FlatAspd = FlatAspdRate = 0;
        FlatMaxHp = FlatMaxSp = MaxHpRate = MaxSpRate = 0;
        LongAtkRate = ShortAtkRate = CritAtkRate = 0;
        VarCastRate = FixCastRate = AddVarCastMs = AddFixCastMs = DelayRate = 0;
        DrainHpRate = DrainSpRate = 0;
    }

    /// <summary>An empty (all-zero) bundle. Convenience for tests / mobs / NPCs that have no equip bonuses.</summary>
    public static EquipBonusBundle Empty { get; } = new();
}

/// <summary>
/// rAthena <c>e_class</c> on monsters / players. Index into the
/// class-bonus arrays on <see cref="EquipBonusBundle"/>. Matches
/// rAthena's <c>CLASS_NORMAL</c>=0, <c>CLASS_BOSS</c>=1,
/// <c>CLASS_GUARDIAN</c>=2, <c>CLASS_ALL</c>=3.
/// </summary>
public enum BattleClassFlag
{
    Normal = 0,
    Boss = 1,
    Guardian = 2,
    All = 3,
}
