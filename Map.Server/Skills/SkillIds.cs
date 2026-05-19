namespace Map.Server.Skills;

/// <summary>
/// rAthena skill-id constants (skill.hpp <c>enum e_skill</c>). Only the
/// starter set the C# server ports first is here; values match rAthena
/// exactly so client-emitted ids resolve without translation.
/// </summary>
public static class SkillIds
{
    public const ushort NV_BASIC = 1;       // novice basic skill family

    public const ushort SM_BASH = 5;        // swordsman: melee damage skill
    public const ushort SM_PROVOKE = 6;     // swordsman: enrage debuff
    public const ushort SM_MAGNUM = 7;      // swordsman: AoE fire
    public const ushort SM_ENDURE = 8;      // swordsman: stun-immune buff

    public const ushort AL_HEAL = 28;       // acolyte: heal-single-target
    public const ushort AL_INCAGI = 29;     // acolyte: SC_INCREASEAGI
    public const ushort AL_DECAGI = 30;     // acolyte: SC_DECREASEAGI
    public const ushort AL_BLESSING = 34;   // acolyte: SC_BLESSING

    public const ushort MG_NAPALMBEAT = 11; // mage: ground magic
    public const ushort MG_COLDBOLT = 14;   // mage: water bolt
    public const ushort MG_FIREBOLT = 19;   // mage: fire bolt
    public const ushort MG_LIGHTNINGBOLT = 20; // mage: wind bolt
    public const ushort MG_SOULSTRIKE = 13; // mage: ghost-element single hit
}
