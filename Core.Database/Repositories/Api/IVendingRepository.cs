using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IVendingRepository
{
    Task<VendingEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<VendingEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<IReadOnlyList<VendingEntity>> GetAllActiveAsync(CancellationToken ct = default);
    Task<VendingEntity> AddAsync(VendingEntity entity, CancellationToken ct = default);
    Task UpdateAsync(VendingEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

