using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class LoginRepository : BaseRepository<LoginEntity>, ILoginRepository
{
    public LoginRepository(GameDbContext context) : base(context)
    {
    }

    public async Task<LoginEntity?> GetByIdAsync(int accountId, CancellationToken ct = default)
    {
        return await DbSet.FindAsync(new object[] { accountId }, ct);
    }

    public async Task<LoginEntity?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.UserId == userId, ct);
    }

    public async Task<LoginEntity?> GetByWebAuthTokenAsync(string token, CancellationToken ct = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.WebAuthToken == token, ct);
    }

    public async Task<IReadOnlyList<LoginEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.ToListAsync(ct);
    }

    public new async Task<LoginEntity> AddAsync(LoginEntity entity, CancellationToken ct = default)
    {
        return await base.AddAsync(entity, ct);
    }

    public new async Task UpdateAsync(LoginEntity entity, CancellationToken ct = default)
    {
        await base.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int accountId, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(accountId, ct);
        if (entity != null)
        {
            await base.DeleteAsync(entity);
        }
    }

    public async Task<bool> ExistsAsync(int accountId, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(e => e.AccountId == accountId, ct);
    }

    public async Task<bool> UserIdExistsAsync(string userId, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(e => e.UserId == userId, ct);
    }

    public Task<LoginEntity?> GetByEmailAsync(string email)
    {
        return DbSet.FirstOrDefaultAsync(e => e.Email == email);
    }
}

