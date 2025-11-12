using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IHomunculusRepository
{
    Task<HomunculusEntity?> GetByIdAsync(int homunId, CancellationToken ct = default);
    Task<HomunculusEntity?> GetByCharIdAsync(int charId, CancellationToken ct = default);
    Task<HomunculusEntity> AddAsync(HomunculusEntity entity, CancellationToken ct = default);
    Task UpdateAsync(HomunculusEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int homunId, CancellationToken ct = default);
}

