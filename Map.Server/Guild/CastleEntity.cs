using System.Collections.Generic;

namespace Map.Server.Guild;

/// <summary>
/// Map-server in-memory replica of a single guild castle. Mirrors
/// rAthena <c>struct guild_castle</c> (guild.hpp) — castle ownership,
/// economy, defense, kafra availability, per-guardian visibility.
///
/// The authoritative copy lives on char-server (guild_castle table);
/// we cache hot fields so WoE-time damage / capture / ownership
/// queries don't have to round-trip per check.
/// </summary>
public sealed class CastleEntity
{
    /// <summary>rAthena <c>castle_id</c>.</summary>
    public int CastleId { get; set; }
    /// <summary>Owning guild id (0 = unowned).</summary>
    public int GuildId { get; set; }
    /// <summary>Map id this castle's emperium room sits on.</summary>
    public uint MapId { get; set; }
    /// <summary>Castle display name (debug + logging).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Current economy investment.</summary>
    public int Economy { get; set; }
    /// <summary>Current defense investment.</summary>
    public int Defense { get; set; }
    /// <summary>Pending economy investment.</summary>
    public int TriggerEconomy { get; set; }
    /// <summary>Pending defense investment.</summary>
    public int TriggerDefense { get; set; }
    /// <summary>Next-pay timestamp.</summary>
    public long NextTime { get; set; }
    /// <summary>Last-pay timestamp.</summary>
    public long PayTime { get; set; }
    /// <summary>Creation timestamp (last conquest).</summary>
    public long CreateTime { get; set; }
    /// <summary>Kafra NPC visibility flag (CD_ENABLED_KAFRA).</summary>
    public int VisibleKafra { get; set; }

    /// <summary>
    /// Per-guardian visibility map. Index = guardian slot
    /// (0..MAX_GUARDIANS-1); value = visible flag.
    /// </summary>
    public Dictionary<int, int> GuardianVisible { get; } = new();
}

/// <summary>
/// Castle data index — mirrors rAthena <c>enum e_castle_data</c>.
/// Used as the <c>index</c> argument to
/// <c>IGuildService.CastleDataSave</c>.
/// </summary>
public static class CastleDataIndex
{
    public const int GuildId = 1;
    public const int CurrentEconomy = 2;
    public const int CurrentDefense = 3;
    public const int InvestedEconomy = 4;
    public const int InvestedDefense = 5;
    public const int NextTime = 6;
    public const int PayTime = 7;
    public const int CreateTime = 8;
    public const int EnabledKafra = 9;
    /// <summary>CD_ENABLED_GUARDIAN00 .. CD_MAX-1; range gate.</summary>
    public const int EnabledGuardian00 = 10;
    public const int MaxGuardians = 8;
    public const int Max = EnabledGuardian00 + MaxGuardians;
}
