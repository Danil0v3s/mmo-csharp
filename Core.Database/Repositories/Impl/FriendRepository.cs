using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class FriendRepository : BaseRepository<FriendEntity>, IFriendRepository
{
    public FriendRepository(GameDbContext context) : base(context) {}
    
    public async Task<IReadOnlyList<FriendEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default) =>
        await DbSet.Where(e => e.CharId == charId).ToListAsync(ct);
    
    public new async Task<FriendEntity> AddAsync(FriendEntity entity, CancellationToken ct = default) =>
        await base.AddAsync(entity, ct);
    
    public async Task DeleteAsync(int charId, int friendId, CancellationToken ct = default) {
        var entity = await DbSet.FindAsync(new object[] { charId, friendId }, ct);
        if (entity != null) await base.DeleteAsync(entity);
    }
    
    public async Task<bool> AreFriendsAsync(int charId, int friendId, CancellationToken ct = default) =>
        await DbSet.AnyAsync(e => e.CharId == charId && e.FriendId == friendId, ct);
}
