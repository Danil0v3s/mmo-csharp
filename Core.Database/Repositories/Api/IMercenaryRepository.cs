using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IMercenaryRepository
{
    Task<MercenaryEntity?> GetByIdAsync(int merId, CancellationToken ct = default);
    Task<MercenaryEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<MercenaryEntity> AddAsync(MercenaryEntity entity, CancellationToken ct = default);
    Task UpdateAsync(MercenaryEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int merId, CancellationToken ct = default);
}

