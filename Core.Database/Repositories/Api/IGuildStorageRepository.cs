using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IGuildStorageRepository
{
    Task<GuildStorageEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<GuildStorageEntity>> GetByGuildIdAsync(int guildId, CancellationToken ct = default);
    Task<GuildStorageEntity> AddAsync(GuildStorageEntity entity, CancellationToken ct = default);
    Task UpdateAsync(GuildStorageEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

