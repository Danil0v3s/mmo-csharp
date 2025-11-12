using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IElementalRepository
{
    Task<ElementalEntity?> GetByIdAsync(int eleId, CancellationToken ct = default);
    Task<ElementalEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<ElementalEntity> AddAsync(ElementalEntity entity, CancellationToken ct = default);
    Task UpdateAsync(ElementalEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int eleId, CancellationToken ct = default);
}

