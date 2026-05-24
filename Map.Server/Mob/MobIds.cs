namespace Map.Server.Mob;

/// <summary>
/// Port of rAthena <c>enum MOBID</c> (mob.hpp:53). Class-id constants
/// for mobs that game code references by name (script-summoned pets,
/// quest mobs, special-AI summons). The full mob catalog comes from
/// <c>mob_db</c> at boot — these constants only cover the symbols
/// referenced from per-skill plugins.
/// </summary>
public static class MobIds
{
    // ---- Plant family (used by GN_HUSKBOMB / GN_THORNS_TRAP / SA_FORTUNE drops) ----
    public const int Poring = 1002;
    public const int RedPlant = 1078;
    public const int BluePlant = 1079;
    public const int GreenPlant = 1080;
    public const int YellowPlant = 1081;
    public const int WhitePlant = 1082;
    public const int ShiningPlant = 1083;
    public const int BlackMushroom = 1084;

    // ---- Alchemist summons ----
    /// <summary>rAthena <c>MOBID_MARINE_SPHERE</c> — Alchemist Marine Sphere (AI_SPHERE).</summary>
    public const int MarineSphere = 1142;

    public const int Emperium = 1288;

    // ---- Geneticist Plants (G_*) — AI_FLORA ----
    public const int GParasite = 1555;
    public const int GFlora = 1575;
    public const int GHydra = 1579;
    public const int GMandragora = 1589;
    public const int GGeographer = 1590;

    // ---- WoE guardians ----
    public const int GuardianStone1 = 1907;
    public const int GuardianStone2 = 1908;

    // ---- Mechanic FAW turrets (AI_FAW) ----
    public const int SilverSniper = 2042;
    public const int MagicDecoyFire = 2043;
    public const int MagicDecoyWater = 2044;
    public const int MagicDecoyEarth = 2045;
    public const int MagicDecoyWind = 2046;

    // ---- Sura summon hornet variants ----
    public const int SHornet = 2158;
    public const int SGiantHornet = 2159;
    public const int SLuciolaVespa = 2160;

    // ---- Ninja Zanzou clone ----
    /// <summary>rAthena <c>MOBID_ZANZOU</c> — KO_ZANZOU clone.</summary>
    public const int Zanzou = 2308;

    public const int GuildSkillFlag = 20269;

    // ---- Sorcerer / Elemental Master ABR pets (AI_ABR) ----
    public const int AbrBattleWarrior = 20834;
    public const int AbrDualCannon = 20835;
    public const int AbrMotherNet = 20836;
    public const int AbrInfinity = 20837;

    // ---- Elemental Master Bionic pets (AI_BIONIC) ----
    public const int BionicWoodenWarrior = 20848;
    public const int BionicWoodenFairy = 20849;
    public const int BionicCreeper = 20850;
    public const int BionicHelltree = 20851;
}
