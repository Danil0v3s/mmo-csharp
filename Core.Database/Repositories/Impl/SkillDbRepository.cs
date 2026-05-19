using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class SkillDbRepository : ISkillDbRepository
{
    private readonly GameDbContext _context;

    public SkillDbRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task<SkillDbEntity?> GetByIdAsync(ushort skillId, CancellationToken ct = default)
        => await _context.SkillDb.AsNoTracking().FirstOrDefaultAsync(s => s.Id == skillId, ct);

    public async Task<SkillDbEntity?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.SkillDb.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task<List<SkillDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await _context.SkillDb.AsNoTracking().ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.SkillDb.AsNoTracking().CountAsync(ct);
}
