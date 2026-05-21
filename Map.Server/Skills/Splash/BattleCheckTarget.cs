namespace Map.Server.Skills.Splash;

/// <summary>
/// Mirror of the rAthena <c>BCT_*</c> bit-mask family
/// (battle.hpp / battle.cpp) used by every <c>skill_area_sub</c> and
/// <c>map_foreachinrange</c> filter. C# code uses
/// <see cref="IMapForeachInRangeService"/> wrappers (ForEachEnemy /
/// ForEachAlly / etc.); the flag set is here for callers that need
/// the union (e.g. "BCT_PARTY | BCT_GUILD").
/// </summary>
[System.Flags]
public enum BattleCheckTarget
{
    None = 0,
    /// <summary>Source itself (BCT_SELF).</summary>
    Self = 0x0001,
    /// <summary>Members of source's party (BCT_PARTY).</summary>
    Party = 0x0002,
    /// <summary>Members of source's guild (BCT_GUILD).</summary>
    Guild = 0x0004,
    /// <summary>Anyone hostile (BCT_ENEMY).</summary>
    Enemy = 0x0008,
    /// <summary>Friendly mob slaves (BCT_NEUTRAL).</summary>
    Neutral = 0x0010,
    /// <summary>BCT_NOENEMY = everyone except enemies (party + guild + neutral + self).</summary>
    NoEnemy = Self | Party | Guild | Neutral,
    /// <summary>BCT_NOPARTY = everyone except own party (used by attack-anyone-but-party skills).</summary>
    NoParty = Self | Guild | Enemy | Neutral,
    /// <summary>BCT_ALL = catch-all.</summary>
    All = Self | Party | Guild | Enemy | Neutral,
}
