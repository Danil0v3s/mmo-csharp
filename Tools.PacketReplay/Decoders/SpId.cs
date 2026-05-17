namespace Tools.PacketReplay.Decoders;

/// <summary>
/// Maps the rAthena <c>SP_*</c> parameter IDs (map.hpp:489) to readable
/// names so per-stat decoders surface "SP_STR" rather than the raw 13.
/// Used by every status-cascade decoder.
/// </summary>
public static class SpId
{
    public static string Name(uint id) => id switch
    {
        0 => "SP_SPEED",
        1 => "SP_BASEEXP",
        2 => "SP_JOBEXP",
        3 => "SP_KARMA",
        4 => "SP_MANNER",
        5 => "SP_HP",
        6 => "SP_MAXHP",
        7 => "SP_SP",
        8 => "SP_MAXSP",
        9 => "SP_STATUSPOINT",
        11 => "SP_BASELEVEL",
        12 => "SP_SKILLPOINT",
        13 => "SP_STR",
        14 => "SP_AGI",
        15 => "SP_VIT",
        16 => "SP_INT",
        17 => "SP_DEX",
        18 => "SP_LUK",
        19 => "SP_CLASS",
        20 => "SP_ZENY",
        21 => "SP_SEX",
        22 => "SP_NEXTBASEEXP",
        23 => "SP_NEXTJOBEXP",
        24 => "SP_WEIGHT",
        25 => "SP_MAXWEIGHT",
        32 => "SP_USTR",
        33 => "SP_UAGI",
        34 => "SP_UVIT",
        35 => "SP_UINT",
        36 => "SP_UDEX",
        37 => "SP_ULUK",
        41 => "SP_ATK1",
        42 => "SP_ATK2",
        43 => "SP_MATK1",
        44 => "SP_MATK2",
        45 => "SP_DEF1",
        46 => "SP_DEF2",
        47 => "SP_MDEF1",
        48 => "SP_MDEF2",
        49 => "SP_HIT",
        50 => "SP_FLEE1",
        51 => "SP_FLEE2",
        52 => "SP_CRITICAL",
        53 => "SP_ASPD",
        55 => "SP_JOBLEVEL",
        99 => "SP_CARTINFO",
        // 4th-job renewal
        219 => "SP_POW",
        220 => "SP_STA",
        221 => "SP_WIS",
        222 => "SP_SPL",
        223 => "SP_CON",
        224 => "SP_CRT",
        225 => "SP_PATK",
        226 => "SP_SMATK",
        227 => "SP_RES",
        228 => "SP_MRES",
        229 => "SP_HPLUS",
        230 => "SP_CRATE",
        231 => "SP_TRAITPOINT",
        232 => "SP_AP",
        233 => "SP_MAXAP",
        247 => "SP_UPOW",
        248 => "SP_USTA",
        249 => "SP_UWIS",
        250 => "SP_USPL",
        251 => "SP_UCON",
        252 => "SP_UCRT",
        1000 => "SP_ATTACKRANGE",
        _ => $"SP_?({id})",
    };

    /// <summary>
    /// Format as "name(id)" — used inside DecodedField values so the
    /// comparer's per-field diff reads "VarId: expected=SP_STR(13) actual=SP_AGI(14)"
    /// instead of two opaque integers.
    /// </summary>
    public static string Format(uint id) => $"{Name(id)}({id})";
}
