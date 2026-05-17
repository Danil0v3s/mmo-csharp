namespace Core.Server.Packets;

/// <summary>
/// rAthena <c>SP_*</c> parameter IDs ([map.hpp:489] <c>enum _sp</c>).
/// Used by every <c>ZC_PAR_CHANGE</c> / <c>ZC_COUPLESTATUS</c> /
/// <c>ZC_LONGLONGPAR_CHANGE</c> packet body.
///
/// The full rAthena enum has ~200 entries — most are bonus-script
/// constants that only the script engine ever passes by name. Only the
/// values the wire protocol actually carries are listed here; gaps are
/// intentional. Add as needed (script_constants enum is out-of-scope
/// until [adjacent/skills.md] / [adjacent/items.md] land).
/// </summary>
public static class SpId
{
    // ─── Block 1: 0-30 (basic stats + meta) ─────────────────────────
    public const ushort SP_SPEED = 0;
    public const ushort SP_BASEEXP = 1;
    public const ushort SP_JOBEXP = 2;
    public const ushort SP_KARMA = 3;
    public const ushort SP_MANNER = 4;
    public const ushort SP_HP = 5;
    public const ushort SP_MAXHP = 6;
    public const ushort SP_SP = 7;
    public const ushort SP_MAXSP = 8;
    public const ushort SP_STATUSPOINT = 9;
    // 10 = SP_0a (unused on wire)
    public const ushort SP_BASELEVEL = 11;
    public const ushort SP_SKILLPOINT = 12;
    public const ushort SP_STR = 13;
    public const ushort SP_AGI = 14;
    public const ushort SP_VIT = 15;
    public const ushort SP_INT = 16;
    public const ushort SP_DEX = 17;
    public const ushort SP_LUK = 18;
    public const ushort SP_CLASS = 19;
    public const ushort SP_ZENY = 20;
    public const ushort SP_SEX = 21;
    public const ushort SP_NEXTBASEEXP = 22;
    public const ushort SP_NEXTJOBEXP = 23;
    public const ushort SP_WEIGHT = 24;
    public const ushort SP_MAXWEIGHT = 25;

    // ─── Block 2: 32-37 (need-points for stat raise) ───────────────
    public const ushort SP_USTR = 32;
    public const ushort SP_UAGI = 33;
    public const ushort SP_UVIT = 34;
    public const ushort SP_UINT = 35;
    public const ushort SP_UDEX = 36;
    public const ushort SP_ULUK = 37;

    // ─── Block 3: 41-55 (derived combat stats) ─────────────────────
    public const ushort SP_ATK1 = 41;
    public const ushort SP_ATK2 = 42;
    public const ushort SP_MATK1 = 43;
    public const ushort SP_MATK2 = 44;
    public const ushort SP_DEF1 = 45;
    public const ushort SP_DEF2 = 46;
    public const ushort SP_MDEF1 = 47;
    public const ushort SP_MDEF2 = 48;
    public const ushort SP_HIT = 49;
    public const ushort SP_FLEE1 = 50;
    public const ushort SP_FLEE2 = 51;
    public const ushort SP_CRITICAL = 52;
    public const ushort SP_ASPD = 53;
    public const ushort SP_JOBLEVEL = 55;

    public const ushort SP_CARTINFO = 99;

    // ─── Block 4: 219-233 (renewal 4-job stats) ────────────────────
    public const ushort SP_POW = 219;
    public const ushort SP_STA = 220;
    public const ushort SP_WIS = 221;
    public const ushort SP_SPL = 222;
    public const ushort SP_CON = 223;
    public const ushort SP_CRT = 224;
    public const ushort SP_PATK = 225;
    public const ushort SP_SMATK = 226;
    public const ushort SP_RES = 227;
    public const ushort SP_MRES = 228;
    public const ushort SP_HPLUS = 229;
    public const ushort SP_CRATE = 230;
    public const ushort SP_TRAITPOINT = 231;
    public const ushort SP_AP = 232;
    public const ushort SP_MAXAP = 233;

    // ─── Block 5: 247-252 (renewal need-points for 4-job stats) ────
    public const ushort SP_UPOW = 247;
    public const ushort SP_USTA = 248;
    public const ushort SP_UWIS = 249;
    public const ushort SP_USPL = 250;
    public const ushort SP_UCON = 251;
    public const ushort SP_UCRT = 252;

    // ─── Block 6: 1000+ (combat range / bonus mechanics — only the
    // ones the wire emits) ────────────────────────────────────────
    public const ushort SP_ATTACKRANGE = 1000;
}
