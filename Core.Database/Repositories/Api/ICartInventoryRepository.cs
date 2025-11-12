using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface ICartInventoryRepository
{
    Task<CartInventoryEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CartInventoryEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<CartInventoryEntity> AddAsync(CartInventoryEntity entity, CancellationToken ct = default);
    Task UpdateAsync(CartInventoryEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

