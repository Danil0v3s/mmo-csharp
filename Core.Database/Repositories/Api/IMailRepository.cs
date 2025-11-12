using Core.Database.Entities;

namespace Core.Database.Repositories.Api;

public interface IMailRepository
{
    Task<MailEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<MailEntity>> GetByDestIdAsync(int destId, CancellationToken ct = default);
    Task<MailEntity> AddAsync(MailEntity entity, CancellationToken ct = default);
    Task UpdateAsync(MailEntity entity, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

