namespace Map.Server.Guild;

/// <summary>
/// Mirrors rAthena <c>enum e_guild_permission</c>
/// (common/mmo.hpp:826). Stored on each <see cref="GuildPosition"/>
/// as a bitmask; <c>IGuildService.HasPermission</c> checks the
/// caller's position against this flag set.
///
/// Position 0 (the master) implicitly has every bit set, mirroring
/// rAthena's <c>GUILD_POS_ALL_MODE</c> default. Lower positions
/// inherit from <c>GUILD_PERM_DEFAULT = GUILD_PERM_ALL</c>; the GM
/// can flip bits via <c>guild_change_position</c>.
/// </summary>
[System.Flags]
public enum GuildPermission
{
    /// <summary>No permissions (e.g. brand-new low-rank slot).</summary>
    None    = 0x000,
    /// <summary>GUILD_PERM_INVITE — may invite new members.</summary>
    Invite  = 0x001,
    /// <summary>GUILD_PERM_EXPEL — may expel members.</summary>
    Expel   = 0x010,
    /// <summary>GUILD_PERM_STORAGE — may open/withdraw from guild storage (renewal only).</summary>
    Storage = 0x100,
    /// <summary>GUILD_PERM_ALL — every bit (renewal).</summary>
    All     = Invite | Expel | Storage,
}
