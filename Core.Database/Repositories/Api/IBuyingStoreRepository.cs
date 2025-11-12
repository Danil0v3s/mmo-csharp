using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IBuyingStoreRepository
{
    Task<BuyingStoreEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BuyingStoreEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<IReadOnlyList<BuyingStoreEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task<BuyingStoreEntity> AddAsync(BuyingStoreEntity entity, CancellationToken ct = default);
    Task UpdateAsync(BuyingStoreEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

