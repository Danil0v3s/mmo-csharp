namespace Map.Server.Skills;

/// <summary>
/// rAthena skill-id constants (skill.hpp <c>enum e_skill</c>, verified
/// against <c>db/re/skill_db.yml</c>). Values match rAthena exactly so
/// client-emitted ids resolve without translation.
///
/// Only the skills with C# ports (resolver pathways, behavior plugins,
/// or runtime references) are listed. Other rAthena skills resolve via
/// <see cref="SkillDb.Get(ushort)"/> by raw id — adding a named constant
/// here is the first step when porting a new skill behavior.
/// </summary>
public static class SkillIds
{
    // ----- Novice -----
    public const ushort NV_BASIC = 1;

    // ----- Swordsman -----
    public const ushort SM_BASH = 5;
    public const ushort SM_PROVOKE = 6;
    public const ushort SM_MAGNUM = 7;
    public const ushort SM_ENDURE = 8;

    // ----- Mage -----
    public const ushort MG_NAPALMBEAT = 11;
    public const ushort MG_SOULSTRIKE = 13;
    public const ushort MG_COLDBOLT = 14;
    public const ushort MG_FROSTDIVER = 15;
    public const ushort MG_STONECURSE = 16;
    public const ushort MG_FIREBALL = 17;
    public const ushort MG_FIREBOLT = 19;
    public const ushort MG_LIGHTNINGBOLT = 20;
    public const ushort MG_THUNDERSTORM = 21;

    // ----- Acolyte -----
    public const ushort AL_HEAL = 28;
    public const ushort AL_INCAGI = 29;
    public const ushort AL_DECAGI = 30;
    public const ushort AL_CRUCIS = 32;
    public const ushort AL_BLESSING = 34;
    public const ushort AL_HOLYLIGHT = 156;

    // ----- Merchant -----
    public const ushort MC_MAMMONITE = 42;

    // ----- Archer -----
    public const ushort AC_DOUBLE = 46;
    public const ushort AC_SHOWER = 47;

    // ----- Thief -----
    public const ushort TF_HIDING = 51;
    public const ushort TF_POISON = 52;

    // ----- Knight (1-1 advance from Swordsman) -----
    public const ushort KN_PIERCE = 56;
    public const ushort KN_BRANDISHSPEAR = 57;
    public const ushort KN_SPEARSTAB = 58;
    public const ushort KN_SPEARBOOMERANG = 59;
    public const ushort KN_TWOHANDQUICKEN = 60;
    public const ushort KN_AUTOCOUNTER = 61;
    public const ushort KN_BOWLINGBASH = 62;

    // ----- Priest (1-2 advance from Acolyte) -----
    public const ushort PR_IMPOSITIO = 66;
    public const ushort PR_SUFFRAGIUM = 67;
    public const ushort PR_ASPERSIO = 68;
    public const ushort PR_KYRIE = 73;
    public const ushort PR_MAGNIFICAT = 74;
    public const ushort PR_GLORIA = 75;
    public const ushort PR_LEXDIVINA = 76;
    public const ushort PR_TURNUNDEAD = 77;
    public const ushort PR_LEXAETERNA = 78;
    public const ushort PR_MAGNUSEXORCISMUS = 79;

    // ----- Wizard (1-2 advance from Mage) -----
    public const ushort WZ_STORMGUST = 89;

    // ----- Blacksmith (1-1 advance from Merchant) -----
    public const ushort BS_HAMMERFALL = 110;
    public const ushort BS_ADRENALINE = 111;
    public const ushort BS_OVERTHRUST = 113;

    // ----- Hunter (1-2 advance from Archer) -----
    public const ushort HT_BLITZBEAT = 129;

    // ----- Assassin (1-2 advance from Thief) -----
    public const ushort AS_SONICBLOW = 136;
    public const ushort AS_GRIMTOOTH = 137;
    public const ushort AS_ENCHANTPOISON = 138;

    // ----- Monk (transcend Acolyte) -----
    public const ushort MO_TRIPLEATTACK = 263;
    public const ushort MO_BODYRELOCATION = 264;
    public const ushort MO_INVESTIGATE = 266;
    public const ushort MO_FINGEROFFENSIVE = 267;
    public const ushort MO_EXPLOSIONSPIRITS = 270;
    public const ushort MO_EXTREMITYFIST = 271;
}
