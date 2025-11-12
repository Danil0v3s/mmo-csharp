using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IPartyRepository
{
    Task<PartyEntity?> GetByIdAsync(int partyId, CancellationToken ct = default);
    Task<PartyEntity?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<PartyEntity>> GetAllAsync(CancellationToken ct = default);
    Task<PartyEntity> AddAsync(PartyEntity entity, CancellationToken ct = default);
    Task UpdateAsync(PartyEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int partyId, CancellationToken ct = default);
}

