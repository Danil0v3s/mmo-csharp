using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IGuildAllianceRepository
{
    Task<IReadOnlyList<GuildAllianceEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default);
    Task<GuildAllianceEntity> AddAsync(GuildAllianceEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int guildId, int allianceId, CancellationToken ct = default);
}

