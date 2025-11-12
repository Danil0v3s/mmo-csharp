using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface ICharacterRepository
{
    Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default);
    Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default);
    Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default);
    Task UpdateAsync(CharEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int charId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int charId, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
}

