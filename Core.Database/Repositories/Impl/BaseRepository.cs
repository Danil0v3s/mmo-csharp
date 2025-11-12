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
        return entity;
    }

    protected Task UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    protected Task DeleteAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}

