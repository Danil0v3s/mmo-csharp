using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IInventoryRepository
{
    Task<InventoryEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryEntity>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryEntity> AddAsync(InventoryEntity entity, CancellationToken ct = default);
    Task UpdateAsync(InventoryEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}

