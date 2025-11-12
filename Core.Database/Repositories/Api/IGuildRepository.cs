using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IGuildRepository
{
    Task<GuildEntity?> GetByIdAsync(int guildId, CancellationToken ct = default);
    Task<GuildEntity?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<GuildEntity>> GetAllAsync(CancellationToken ct = default);
    Task<GuildEntity> AddAsync(GuildEntity entity, CancellationToken ct = default);
    Task UpdateAsync(GuildEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int guildId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int guildId, CancellationToken ct = default);
}

