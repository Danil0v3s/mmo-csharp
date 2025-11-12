using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class CharacterRepository : BaseRepository<CharEntity>, ICharacterRepository
{
    public CharacterRepository(GameDbContext context) : base(context)
    {
    }

    public async Task<CharEntity?> GetByIdAsync(int charId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(c => c.Inventories)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.CharId == charId, ct);
    }

    public async Task<CharEntity?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.Name == name, ct);
    }

    public async Task<IReadOnlyList<CharEntity>> GetByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        return await DbSet
            .Where(c => c.AccountId == accountId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CharEntity>> GetOnlineCharactersAsync(CancellationToken ct = default)
    {
        return await DbSet
            .Where(c => c.Online != 0)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CharEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    public new async Task<CharEntity> AddAsync(CharEntity entity, CancellationToken ct = default)
    {
        return await base.AddAsync(entity, ct);
    }

    public new async Task UpdateAsync(CharEntity entity, CancellationToken ct = default)
    {
        await base.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int charId, CancellationToken ct = default)
    {
        var entity = await DbSet.FindAsync(new object[] { charId }, ct);
        if (entity != null)
        {
            await base.DeleteAsync(entity);
        }
    }

    public async Task<bool> ExistsAsync(int charId, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(c => c.CharId == charId, ct);
    }

    public async Task<bool> NameExistsAsync(string name, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(c => c.Name == name, ct);
    }
}

