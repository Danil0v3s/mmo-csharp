using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

/// <summary>
/// Query surface for the guild-storage audit log. Mirrors rAthena
/// <c>storage_guild_log_read</c> / <c>storage_guild_log_read_sub</c>
/// (storage.cpp): paginated reads filtered by guild, optionally bracketed
/// by item-id and date range. The write path is the existing
/// <see cref="IGuildStorageService.Log"/> inserter; this is the read side
/// that surfaces the audit trail to GM commands / web reports.
/// </summary>
public interface IGuildStorageLogRepository
{
    /// <summary>Most-recent-first audit entries for the guild. <paramref name="limit"/>
    /// caps the row count (rAthena default = 100).</summary>
    Task<IReadOnlyList<GuildStorageLogEntity>> GetByGuildIdAsync(int guildId, int limit = 100, CancellationToken ct = default);

    /// <summary>rAthena <c>storage_guild_log_read_sub</c>. Same as
    /// <see cref="GetByGuildIdAsync"/> but filtered by a specific item-id
    /// — used when a GM asks "who pulled item X from the guild?".</summary>
    Task<IReadOnlyList<GuildStorageLogEntity>> GetByGuildAndItemAsync(int guildId, uint nameId, int limit = 100, CancellationToken ct = default);

    /// <summary>Append a single audit row. Persisted side of
    /// <see cref="IGuildStorageService.Log"/> — call this from the
    /// char-side intif loop when the IPC log packet lands.</summary>
    Task<GuildStorageLogEntity> AddAsync(GuildStorageLogEntity entity, CancellationToken ct = default);
}
