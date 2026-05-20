using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class MobSkillDbRepository : IMobSkillDbRepository
{
    private readonly GameDbContext _context;

    public MobSkillDbRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MobSkillDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await _context.MobSkillDb.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<MobSkillDbEntity>> GetByMobIdAsync(int mobId, CancellationToken ct = default)
        => await _context.MobSkillDb
            .AsNoTracking()
            .Where(m => m.MobId == mobId)
            .ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.MobSkillDb.CountAsync(ct);
}
