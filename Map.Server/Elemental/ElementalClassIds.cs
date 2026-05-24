namespace Map.Server.Elemental;

/// <summary>
/// rAthena <c>ELEMENTALID_*</c> — class-id table for every Sorcerer /
/// Elemental Master bound elemental (elemental.hpp:135). Used by the
/// SummonElemental* skills to look up the tier they should
/// instantiate and the EM-tier upgrade skills to verify the caster
/// already has the prerequisite L-tier active.
/// </summary>
public static class ElementalClassIds
{
    // Agni (Fire) family.
    public const int AgniS = 2114;
    public const int AgniM = 2115;
    public const int AgniL = 2116;

    // Aqua (Water) family.
    public const int AquaS = 2117;
    public const int AquaM = 2118;
    public const int AquaL = 2119;

    // Ventus (Wind) family.
    public const int VentusS = 2120;
    public const int VentusM = 2121;
    public const int VentusL = 2122;

    // Tera (Earth) family.
    public const int TeraS = 2123;
    public const int TeraM = 2124;
    public const int TeraL = 2125;

    // EM-tier specials — promotions from the matching L-tier base.
    public const int Diluvio = 2126;   // ← AquaL
    public const int Ardor = 2127;     // ← AgniL
    public const int Procella = 2128;  // ← VentusL
    public const int Terremotus = 2129; // ← TeraL
    public const int Serpens = 2130;   // ← any L

    /// <summary>True when <paramref name="classId"/> is one of the four
    /// L-tier elemental classes (the prerequisite tier for EM
    /// promotions and any-L gated buffs).</summary>
    public static bool IsLTier(int classId)
        => classId == AgniL || classId == AquaL || classId == VentusL || classId == TeraL;

    /// <summary>rAthena lifetime constant for SO_SUMMON_AGNI etc.
    /// (skill.cpp::skill_castend_nodamage_id, 10 minutes baseline,
    /// reduced by penalty). First slice uses the flat value.</summary>
    public const int DefaultLifetimeMs = 600_000;
}
