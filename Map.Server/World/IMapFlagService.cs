namespace Map.Server.World;

/// <summary>
/// Lookup for the per-map flag set declared via <c>registerMapFlag()</c>
/// in TS scripts (or rAthena's <c>npc/re/mapflag/*.txt</c>). Pre-built
/// once at startup from <c>INpcRegistry.AllMapFlags()</c>.
///
/// rAthena reference: <c>mapflag.yml</c> / <c>map.cpp:map_setmapflag</c>.
/// A consumer (combat / movement / skills / pickup) calls
/// <see cref="IsSet"/> at the relevant gate to honor the flag.
/// </summary>
public interface IMapFlagService
{
    /// <summary>True if the named flag is active for <paramref name="mapName"/>.</summary>
    bool IsSet(string mapName, MapFlag flag);
}

/// <summary>
/// rAthena <c>mapflag.yml</c> enum mirror — only the flags the C# port
/// currently honors. Bare switches (no value); valued flags will land
/// alongside their consumers.
/// </summary>
public enum MapFlag
{
    /// <summary>Player vs player damage disabled.</summary>
    NoPvp,
    /// <summary>Skill casts refused.</summary>
    NoSkill,
    /// <summary>@warp / scripted teleport refused.</summary>
    NoTeleport,
    /// <summary>Dropping items via CZ_ITEM_THROW refused.</summary>
    NoDrop,
    /// <summary>Mob drops never spawn floor items.</summary>
    NoLoot,
    /// <summary>EXP gain on kill suppressed.</summary>
    NoExp,
    /// <summary>Death EXP penalty suppressed.</summary>
    NoPenalty,
    /// <summary>Save / autosave suppressed for sessions on this map.</summary>
    NoSave,
    /// <summary>Trade (player↔player exchange) refused.</summary>
    NoTrade,
    /// <summary>Public chat refused.</summary>
    NoChat,
    /// <summary>Vending refused (player shops).</summary>
    NoVending,
    /// <summary>Buying store refused (player buy stalls).</summary>
    NoBuyingStore,
    /// <summary>NPC `monster` summon refused.</summary>
    NoBranch,
    /// <summary>Memo skill refused (saves a warp point).</summary>
    NoMemo,
    /// <summary>GvG-zone damage rules apply.</summary>
    Gvg,
    /// <summary>SC application refused (rAthena <c>nostatus</c> mapflag).</summary>
    NoStatus,
    // SKILL-03 — PvP / friendly-fire allegiance flags. Appended (the flag-bit
    // store keys off the enum ordinal, so new members must go at the end).
    /// <summary>Player-vs-player enabled (rAthena <c>MF_PVP</c>). Unaffiliated PCs are mutually attackable.</summary>
    Pvp,
    /// <summary>Battleground map (rAthena <c>MF_BATTLEGROUND</c>) — team-based hostile zone.</summary>
    Battleground,
    /// <summary>PvP friendly-fire on party (rAthena <c>MF_PVP_NOPARTY</c>): party members ARE attackable.</summary>
    PvpNoparty,
    /// <summary>PvP friendly-fire on guild (rAthena <c>MF_PVP_NOGUILD</c>): guild members ARE attackable.</summary>
    PvpNoguild,
    /// <summary>GvG friendly-fire on party (rAthena <c>MF_GVG_NOPARTY</c>): party members ARE attackable.</summary>
    GvgNoparty,
}
