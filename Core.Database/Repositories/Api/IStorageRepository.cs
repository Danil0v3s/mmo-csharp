using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IStorageRepository
{
    Task<StorageEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<StorageEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<StorageEntity> AddAsync(StorageEntity entity, CancellationToken ct = default);
    Task UpdateAsync(StorageEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

