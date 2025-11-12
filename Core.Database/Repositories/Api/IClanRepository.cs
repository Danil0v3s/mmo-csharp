using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IClanRepository
{
    Task<ClanEntity?> GetByIdAsync(int clanId, CancellationToken ct = default);
    Task<ClanEntity?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<ClanEntity>> GetAllAsync(CancellationToken ct = default);
    Task<ClanEntity> AddAsync(ClanEntity entity, CancellationToken ct = default);
    Task UpdateAsync(ClanEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int clanId, CancellationToken ct = default);
}

