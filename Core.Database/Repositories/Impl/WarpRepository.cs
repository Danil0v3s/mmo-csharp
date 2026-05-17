using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class WarpRepository : IWarpRepository
{
    private readonly GameDbContext _context;

    public WarpRepository(GameDbContext context)
    {
        _context = context;
    }

    public Task<List<WarpEntity>> GetBySrcMapAsync(string mapName, CancellationToken ct = default)
        => _context.Warps
            .AsNoTracking()
            .Where(w => w.SrcMap == mapName)
            .ToListAsync(ct);

    public Task<List<WarpEntity>> GetAllAsync(CancellationToken ct = default)
        => _context.Warps
            .AsNoTracking()
            .ToListAsync(ct);
}
