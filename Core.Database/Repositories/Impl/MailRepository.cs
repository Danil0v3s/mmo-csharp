using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class MailRepository : BaseRepository<MailEntity>, IMailRepository
{
    public MailRepository(GameDbContext context) : base(context) {}
    
    public async Task<MailEntity?> GetByIdAsync(long id, CancellationToken ct = default) =>
        await DbSet.Include(m => m.Attachments).FirstOrDefaultAsync(m => m.Id == id, ct);
    
    public async Task<IReadOnlyList<MailEntity>> GetByDestIdAsync(int destId, CancellationToken ct = default) =>
        await DbSet.Where(m => m.DestId == destId).OrderByDescending(m => m.Time).ToListAsync(ct);
    
    public new async Task<MailEntity> AddAsync(MailEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(MailEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(long id, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { id }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
