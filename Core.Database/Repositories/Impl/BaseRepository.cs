using Core.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal abstract class BaseRepository<TEntity> where TEntity : class
{
    protected readonly GameDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(GameDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    protected async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    protected async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(ct);
    }

    protected async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync(ct);
    }
}
