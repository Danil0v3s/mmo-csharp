using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IPetRepository
{
    Task<PetEntity?> GetByIdAsync(int petId, CancellationToken ct = default);
    Task<IReadOnlyList<PetEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<PetEntity> AddAsync(PetEntity entity, CancellationToken ct = default);
    Task UpdateAsync(PetEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int petId, CancellationToken ct = default);
}

