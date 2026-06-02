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

    // COMBAT-06 — damage / defense rate bonuses.
    /// <summary>Weapon-damage percent (<c>bonus bAtkRate, N</c> / SP_ATK_RATE; pre-skill-ratio).</summary>
    public int AtkRate { get; set; }
    /// <summary>Magic-damage percent (<c>bonus bMatkRate, N</c> / SP_MATK_RATE).</summary>
    public int MatkRate { get; set; }
    /// <summary>Flat hard-DEF bonus (<c>bonus bDef, N</c> / SP_DEF).</summary>
    public int FlatDef { get; set; }
    /// <summary>Flat hard-MDEF bonus (<c>bonus bMdef, N</c> / SP_MDEF).</summary>
    public int FlatMdef { get; set; }
    /// <summary>Hard-DEF percent (<c>bonus bDefRate, N</c> / SP_DEF_RATE).</summary>
    public int DefRate { get; set; }
    /// <summary>Hard-MDEF percent (<c>bonus bMdefRate, N</c> / SP_MDEF_RATE).</summary>
    public int MdefRate { get; set; }

    // Primary + trait param bonuses (bStr..bLuk, bPow..bCrt). Mirror
    // rAthena indexed_bonus.param_bonus[] (status.hpp). Populated by the
    // extractor / script host; APPLIED to the final stat by the base→final
    // stat layering in COMBAT-10 (CalcPc cannot fold these idempotently
    // today because s.Str doubles as the base allocated stat read back by
    // every recalc caller — see COMBAT-10). Captured here so the data path
    // is ready and the extractor never silently drops a bStr card.
    public int Str { get; set; }
    public int Agi { get; set; }
    public int Vit { get; set; }
    public int IntStat { get; set; }
    public int Dex { get; set; }
    public int Luk { get; set; }
    public int Pow { get; set; }
    public int Sta { get; set; }
    public int Wis { get; set; }
    public int Spl { get; set; }
    public int Con { get; set; }
    public int Crt { get; set; }

    // Cast-time / delay knobs consumed by SkillCastTimingService.
    public int VarCastRate { get; set; }     // %
    public int FixCastRate { get; set; }     // %
    public int AddVarCastMs { get; set; }    // ms
    public int AddFixCastMs { get; set; }    // ms
    public int DelayRate { get; set; }       // %

    // COMBAT-08 — bNoCastCancel / bNoCastCancel2. When set, an in-progress
    // cast is NOT aborted on taking damage (DamageService cast-interrupt
    // gate). rAthena: sd->state.no_castcancel (status.cpp bonus parse).
    // The bonus parse that flips this lives in COMBAT-23 (skill-state flag
    // batch); the consumer (the cancel gate) is wired here in COMBAT-08.
    public bool NoCastCancel { get; set; }

    // COMBAT-17 — double-attack proc rate (%). rAthena
    // `bonus bDoubleRate, n;` (SP_DOUBLE_RATE, pc.cpp:3924) sets
    // `sd->bonus.double_rate = max(double_rate, n)` — it takes the MAX
    // across all equip sources, NOT a sum. Consumed by
    // BattleCalculator.CalcMultiAttack (battle.cpp:4440) as one of the
    // auto-attack double-attack triggers (`max(7*lv, double_rate)`).
    public int DoubleRate { get; set; }

    // Drain (renewal: hp/sp drained per hit).
    public int DrainHpRate { get; set; }     // % chance × 100
    public int DrainSpRate { get; set; }     // % chance × 100

    // Wave 65 — Coma proc tables (PC kills target instantly to 1 HP).
    // Indexed by (int)BattleRace / BattleClassFlag; the All slot is
    // additive on top of the specific-race / specific-class slots.
    // Sourced from `bonus2 bComaClass, c, rate;` and
    // `bonus2 bComaRace, r, rate;` in card / equip scripts. rAthena
    // stores these as fixed arrays on `sd->bonus` (status.hpp:1980-ish).
    public short[] ComaClass { get; } = new short[ClassSize];
    public short[] ComaRace  { get; } = new short[RaceSize];

    // Wave 65 — AddEff proc tables (status proc on hit / when hit).
    // The autocast spell tables already live on PlayerBonusService as
    // Autobonus entries; AddEff is the SC-side analog — a list of
    // (SC type, rate, duration) entries that fire on hit. rAthena
    // `bonus3 bAddEff, sc, rate, dur;` (against the target on attack)
    // and `bonus3 bAddEffWhenHit, sc, rate, dur;` (against the attacker
    // when receiving damage). The lists are append-only per build and
    // get cleared in Reset() before each rebuild.
    public List<AddEffEntry> AddEffOnAttack { get; } = new();
    public List<AddEffEntry> AddEffWhenHit  { get; } = new();

    /// <summary>Reset all fields. Cheap allocation-free recycle.</summary>
    public void Reset()
    {
        Array.Clear(AddRace); Array.Clear(SubRace);
        Array.Clear(AddEle); Array.Clear(SubEle);
        Array.Clear(AddSize); Array.Clear(SubSize);
        Array.Clear(AddClass); Array.Clear(SubClass);
        Array.Clear(ComaClass); Array.Clear(ComaRace);
        AddEffOnAttack.Clear(); AddEffWhenHit.Clear();
        FlatAtk = FlatMatk = FlatCritical = FlatHit = FlatFlee = 0;
        FlatAspd = FlatAspdRate = 0;
        FlatMaxHp = FlatMaxSp = MaxHpRate = MaxSpRate = 0;
        LongAtkRate = ShortAtkRate = CritAtkRate = 0;
        AtkRate = MatkRate = FlatDef = FlatMdef = DefRate = MdefRate = 0;
        Str = Agi = Vit = IntStat = Dex = Luk = 0;
        Pow = Sta = Wis = Spl = Con = Crt = 0;
        VarCastRate = FixCastRate = AddVarCastMs = AddFixCastMs = DelayRate = 0;
        NoCastCancel = false;
        DoubleRate = 0;
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
    Max = 4,
}

/// <summary>
/// One AddEff slot — fires a status change with the given rate on
/// every hit landed (AddEffOnAttack) or received (AddEffWhenHit).
/// Sourced from <c>bonus3 bAddEff{,WhenHit}, sc, rate, dur;</c> in
/// item / card scripts. rAthena reference: pc.cpp:5440 (pc_bonus3 case
/// SP_ADDEFF) — rate is permille (out of 10 000), dur in ms.
/// </summary>
public readonly record struct AddEffEntry(StatusType Sc, short RatePermille, uint DurationMs);
