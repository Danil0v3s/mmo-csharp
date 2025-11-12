using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IMarketRepository
{
    Task<IReadOnlyList<MarketEntity>> GetByNameAsync(string name, CancellationToken ct = default);
    Task<MarketEntity> AddAsync(MarketEntity entity, CancellationToken ct = default);
    Task UpdateAsync(MarketEntity entity, CancellationToken ct = default);
    Task DeleteAsync(string name, uint nameId, CancellationToken ct = default);
}

