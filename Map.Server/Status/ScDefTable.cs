using System.Collections.Generic;
using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>Which battle stat feeds an SC's resistance term.</summary>
public enum ScResistStat { None, Str, Agi, Vit, IntStat, Dex, Luk, Mdef }

/// <summary>
/// SKILL-01 — the renewal <c>status_get_sc_def</c> resistance switch
/// (status.cpp:9392, <c>RENEWAL</c> branches) expressed as data. Each entry
/// describes how a debuff's landing chance + duration are reduced by the
/// target's stats.
///
/// <para>Renewal rate resistance is <c>sc_def = Stat·StatMul + Level·LevelMul
/// + Luk·LukMul − (levelAdv if <see cref="UseLevelAdv"/>)</c>, where
/// <c>levelAdv = (max(0, lvSrc − lvTgt))² / 5 · 100</c>. Duration gets a flat
/// <see cref="TickDef2Ms"/> adjustment (rAthena <c>tick_def2</c>; subtracted,
/// so a negative value lengthens — faithful to the source).</para>
///
/// Only the SCs whose <see cref="StatusType"/> exists and whose renewal
/// formula is the standard/composite shape are listed; bespoke-formula SCs
/// (JointBeat/DeepSleep/Netherworld/Stasis/OblivionCurse/the GC poison
/// family/…) and the per-SC <c>min_rate</c>/<c>min_duration</c> from
/// status.yml are SKILL-14.
/// </summary>
public readonly record struct ScDefEntry(
    ScResistStat Stat,
    int StatMul,
    bool UseLevelAdv,
    int LevelMul,
    int LukMul,
    int TickDef2Ms,
    bool BossResist,
    bool MvpResist,
    bool CurseLukImmune);

public static class ScDefTable
{
    private static ScDefEntry Std(ScResistStat stat, int tickDef2)
        => new(stat, StatMul: 100, UseLevelAdv: true, LevelMul: 0, LukMul: 0,
               TickDef2Ms: tickDef2, BossResist: true, MvpResist: false, CurseLukImmune: false);

    private static ScDefEntry Composite(ScResistStat stat, int tickDef2)
        => new(stat, StatMul: 20, UseLevelAdv: false, LevelMul: 20, LukMul: 10,
               TickDef2Ms: tickDef2, BossResist: true, MvpResist: false, CurseLukImmune: false);

    // Renewal status_get_sc_def (#ifdef RENEWAL branches), status.cpp:9392+.
    private static readonly Dictionary<StatusType, ScDefEntry> _table = new()
    {
        [StatusType.Poison]    = Std(ScResistStat.Vit,     -2000),  // sc_def = vit*100 - levelAdv
        [StatusType.Stun]      = Std(ScResistStat.Vit,      -500),
        [StatusType.Silence]   = Std(ScResistStat.IntStat, -2000),
        [StatusType.Bleeding]  = Std(ScResistStat.Agi,    -12000),
        [StatusType.Sleep]     = Std(ScResistStat.Agi,     -2000),
        [StatusType.Stonewait] = Std(ScResistStat.Mdef,    -3000),
        [StatusType.Freeze]    = Std(ScResistStat.Mdef,    -3000),
        [StatusType.Blind]     = Std(ScResistStat.IntStat, -2000),
        // Curse: luk*100 - levelAdv, and 100% immune when target LUK == 0.
        [StatusType.Curse]     = Std(ScResistStat.Luk,     -2000) with { CurseLukImmune = true },
        [StatusType.Confusion] = Std(ScResistStat.Luk,     -2000),
        // Composite forms: stat*20 + lv*20 + luk*10 (no levelAdv).
        [StatusType.Fear]      = Composite(ScResistStat.IntStat, -4000),
        [StatusType.Burning]   = Composite(ScResistStat.Agi,     -2000),
    };

    /// <summary>The resist descriptor for <paramref name="type"/>, or null when the SC
    /// is not stat-resistible (buff path: lands at the passed rate, no reduction).</summary>
    public static ScDefEntry? For(StatusType type)
        => _table.TryGetValue(type, out var e) ? e : (ScDefEntry?)null;

    public static int Count => _table.Count;

    /// <summary>Read a battle stat by selector.</summary>
    public static int StatValue(BattleStats s, ScResistStat stat) => stat switch
    {
        ScResistStat.Str => s.Str,
        ScResistStat.Agi => s.Agi,
        ScResistStat.Vit => s.Vit,
        ScResistStat.IntStat => s.IntStat,
        ScResistStat.Dex => s.Dex,
        ScResistStat.Luk => s.Luk,
        ScResistStat.Mdef => s.Mdef,
        _ => 0,
    };
}
