using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class QuestRepository : BaseRepository<QuestEntity>, IQuestRepository
{
    public QuestRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<QuestEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public async Task<QuestEntity?> GetByCharIdAndQuestIdAsync(int charId, uint questId, CancellationToken ct = default) =>
        await DbSet.FindAsync(new object[] { charId, questId }, ct);
    
    public new async Task<QuestEntity> AddAsync(QuestEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public new async Task UpdateAsync(QuestEntity entity, CancellationToken ct = default) =>
        await base.UpdateAsync(entity);
    
    public async Task DeleteAsync(int charId, uint questId, CancellationToken ct = default) {
        var entity = await GetByCharIdAndQuestIdAsync(charId, questId, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
}
