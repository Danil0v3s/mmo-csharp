namespace Map.Server.Entities;

/// <summary>
/// Port of rAthena's <c>MAPID_*</c> macros (mmo.hpp:550..620) — bit
/// masks that compress every Aegis job id down to a (base, tier)
/// pair. Used by every formula that needs to branch on job family
/// without enumerating ~80 individual ids.
///
/// <para>Convention: the low 12 bits hold the base class
/// (<see cref="FirstMask"/>), the upper bits hold tier flags
/// (<see cref="Upper"/>, <see cref="Baby"/>, <see cref="ThirdClass"/>,
/// <see cref="FourthClass"/>). A "Lord Knight" is
/// <c>Swordman | Upper</c>; a "Baby Wizard" is <c>Mage | Baby</c>; a
/// "Sura" is <c>Acolyte | Upper | ThirdClass</c>.</para>
///
/// <para>Most consumers only care about the base — "is this any kind
/// of Gunslinger?" → <c>(pc.ClassMask &amp; MapidClass.FirstMask) ==
/// MapidClass.Gunslinger</c>. The <see cref="FromClassId"/> helper
/// performs the canonical reduction. Add the mask to
/// <see cref="PlayerEntity.ClassMask"/> via
/// <c>pc.ClassMask = MapidClass.FromClassId(pc.ClassId);</c> on every
/// hydration / job change.</para>
/// </summary>
public static class MapidClass
{
    // --- tier flags (high bits) ---
    public const uint Upper       = 0x1000; // 2-1, 2-2, transcendent
    public const uint Baby        = 0x2000;
    public const uint ThirdClass  = 0x4000;
    public const uint FourthClass = 0x8000;

    /// <summary>Mask isolating the base-class nibble — rAthena <c>MAPID_BASEMASK</c>.</summary>
    public const uint FirstMask = 0x00F;
    /// <summary>Mask isolating the upper-tier and 2-class bits — rAthena <c>MAPID_UPPERMASK</c>.</summary>
    public const uint UpperMask = 0xFFF;

    // --- base classes (rAthena MAPID_NOVICE..MAPID_GANGSI) ---
    public const uint Novice      = 0x0;
    public const uint Swordman    = 0x1;
    public const uint Mage        = 0x2;
    public const uint Archer      = 0x3;
    public const uint Acolyte     = 0x4;
    public const uint Merchant    = 0x5;
    public const uint Thief       = 0x6;
    public const uint Taekwon     = 0x7;
    public const uint Wedding     = 0x8;
    public const uint Gunslinger  = 0x9;
    public const uint Ninja       = 0xA;
    public const uint Summoner    = 0xB;
    public const uint Gangsi      = 0xC;

    // --- common composed classes (1st-class up, 2-1 / 2-2, transcendent, 3rd) ---
    public const uint SuperNovice = Novice | Upper;
    public const uint Knight      = Swordman | Upper;
    public const uint Crusader    = Swordman | (Upper << 1); // 2-2 differentiator unused below 4th class today
    public const uint Wizard      = Mage     | Upper;
    public const uint Sage        = Mage     | (Upper << 1);
    public const uint Hunter      = Archer   | Upper;
    public const uint BardDancer  = Archer   | (Upper << 1);
    public const uint Priest      = Acolyte  | Upper;
    public const uint Monk        = Acolyte  | (Upper << 1);
    public const uint Blacksmith  = Merchant | Upper;
    public const uint Alchemist   = Merchant | (Upper << 1);
    public const uint Assassin    = Thief    | Upper;
    public const uint Rogue       = Thief    | (Upper << 1);
    public const uint StarGladiator = Taekwon | Upper;
    public const uint SoulLinker  = Taekwon  | (Upper << 1);
    public const uint Rebellion   = Gunslinger | Upper;
    public const uint KagerouOboro = Ninja | Upper;

    /// <summary>
    /// rAthena <c>pc_jobid2mapid</c> (pc.cpp:6193) — Aegis class id →
    /// MAPID_* bitmask. Covers the first-class set + the common
    /// promotions. Unmapped ids return 0 (Novice base) to match
    /// rAthena's "unknown class falls through" behavior.
    ///
    /// <para>This is the canonical reduction every formula uses
    /// (<c>(class_ &amp; MAPID_FIRSTMASK)</c>). Trans / Baby / 3rd /
    /// 4th tier promotions are layered on by the job-change pipeline
    /// (<c>JobChangeService</c>) rather than here, so the table stays
    /// small and the upgrades remain explicit.</para>
    /// </summary>
    public static uint FromClassId(int classId) => classId switch
    {
        // First-class set — rAthena enum JOB_NOVICE..JOB_THIEF
        0 => Novice,
        1 => Swordman,
        2 => Mage,
        3 => Archer,
        4 => Acolyte,
        5 => Merchant,
        6 => Thief,
        // 2-1 / 2-2 class-up — Aegis ids in rAthena pc.hpp
        7  => Knight,        // JOB_KNIGHT
        8  => Priest,        // JOB_PRIEST
        9  => Wizard,        // JOB_WIZARD
        10 => Blacksmith,    // JOB_BLACKSMITH
        11 => Hunter,        // JOB_HUNTER
        12 => Assassin,      // JOB_ASSASSIN
        14 => Crusader,      // JOB_CRUSADER
        15 => Monk,          // JOB_MONK
        16 => Sage,          // JOB_SAGE
        17 => Rogue,         // JOB_ROGUE
        18 => Alchemist,     // JOB_ALCHEMIST
        19 => BardDancer,    // JOB_BARD
        20 => BardDancer,    // JOB_DANCER
        // Super Novice
        23 => SuperNovice,   // JOB_SUPER_NOVICE
        // Expanded classes — rAthena Gunslinger / Ninja split
        24 => Gunslinger,    // JOB_GUNSLINGER
        25 => Ninja,         // JOB_NINJA
        // Taekwon family
        4046 => Taekwon,     // JOB_TAEKWON
        4047 => StarGladiator,
        4049 => SoulLinker,
        // Doram / Summoner
        4218 => Summoner,    // JOB_SUMMONER
        // Rebellion + Kagerou/Oboro lifted-tier
        4215 => KagerouOboro, // JOB_KAGEROU
        4216 => KagerouOboro, // JOB_OBORO
        4217 => Rebellion,   // JOB_REBELLION
        _ => Novice,
    };

    /// <summary>
    /// Convenience: returns true iff <paramref name="classMask"/>'s
    /// base nibble equals <paramref name="baseClass"/>. Mirrors the
    /// rAthena pattern <c>(class_ &amp; MAPID_FIRSTMASK) == MAPID_X</c>.
    /// </summary>
    public static bool IsBase(uint classMask, uint baseClass)
        => (classMask & FirstMask) == baseClass;
}
