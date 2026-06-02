namespace Map.Server.Status;

/// <summary>
/// Mirrors rAthena <c>enum e_race</c> (map.hpp:318). Used by attacker /
/// defender race-modifier lookups in <see cref="BattleCalculator"/>.
/// Values are kept in lockstep so race tables seeded from rAthena indices
/// align without re-mapping.
/// </summary>
public enum BattleRace : sbyte
{
    None = -1,
    Formless = 0,
    Undead,
    Brute,
    Plant,
    Insect,
    Fish,
    Demon,
    Demihuman,
    Angel,
    Dragon,
    PlayerHuman,
    PlayerDoram,
    All,
    Max,
}

/// <summary>
/// Mirrors rAthena <c>enum e_element</c> (map.hpp:380). The attack table
/// in <see cref="ElementTable"/> is indexed by attacker × defender.
/// </summary>
public enum BattleElement : sbyte
{
    None = -1,
    Neutral = 0,
    Water,
    Earth,
    Fire,
    Wind,
    Poison,
    Holy,
    Dark,
    Ghost,
    Undead,
    All,
    Max,
    // COMBAT-19 — skill-element sentinels (rAthena e_element ELE_WEAPON=12,
    // ELE_ENDOWED=13, ELE_RANDOM=14, map.hpp:394). These are NOT real elements
    // and must never index an element array (size = Max); they are resolved to
    // a concrete element per cast by BattleElementService before use.
    Weapon,   // skill takes the attacker's weapon element
    Endowed,  // skill takes the attacker's endow-SC element
    Random,   // skill rolls a random element each cast
}

/// <summary>
/// Mirrors rAthena <c>enum e_size</c> (mob.hpp:113). Size modifiers in
/// the weapon damage formula scale by attacker weapon vs defender size.
/// </summary>
public enum BattleSize : byte
{
    Small = 0,
    Medium,
    Large,
    All,
}

/// <summary>
/// Mirrors rAthena <c>enum e_mode</c> (common/mmo.hpp:242). Only the
/// subset used by current AI/combat code is wired; the rest are reserved
/// for parity with future ports.
/// </summary>
[Flags]
public enum MobMode : int
{
    None              = 0x0000000,
    CanMove           = 0x0000001,
    Looter            = 0x0000002,
    Aggressive        = 0x0000004,
    Assist            = 0x0000008,
    CastSensorIdle    = 0x0000010,
    NoRandomWalk      = 0x0000020,
    NoCast            = 0x0000040,
    CanAttack         = 0x0000080,
    CastSensorChase   = 0x0000200,
    ChangeChase       = 0x0000400,
    Angry             = 0x0000800,
    ChangeTargetMelee = 0x0001000,
    ChangeTargetChase = 0x0002000,
    TargetWeak        = 0x0004000,
    RandomTarget      = 0x0008000,
    IgnoreMelee       = 0x0010000,
    IgnoreMagic       = 0x0020000,
    IgnoreRanged      = 0x0040000,
    Mvp               = 0x0080000,
    IgnoreMisc        = 0x0100000,
    KnockbackImmune   = 0x0200000,
    TeleportBlock     = 0x0400000,
    FixedItemDrop     = 0x1000000,
    Detector          = 0x2000000,
    StatusImmune      = 0x4000000,
    SkillImmune       = 0x8000000,
}
