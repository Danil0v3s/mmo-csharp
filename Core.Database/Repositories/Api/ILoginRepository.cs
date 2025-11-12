using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface ILoginRepository
{
    Task<LoginEntity?> GetByIdAsync(int accountId, CancellationToken ct = default);
    Task<LoginEntity?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<LoginEntity?> GetByWebAuthTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<LoginEntity>> GetAllAsync(CancellationToken ct = default);
    Task<LoginEntity> AddAsync(LoginEntity entity, CancellationToken ct = default);
    Task UpdateAsync(LoginEntity entity, CancellationToken ct = default);
    Task DeleteAsync(int accountId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int accountId, CancellationToken ct = default);
    Task<bool> UserIdExistsAsync(string userId, CancellationToken ct = default);
    Task<LoginEntity?> GetByEmailAsync(string email);
}

