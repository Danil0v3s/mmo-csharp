using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class MobRepository : IMobRepository
{
     private readonly GameDbContext _context;

    public MobRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task<MobEntity?> GetByIdAsync(uint mobId, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mobId, ct);
    }

    public async Task<MobEntity?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.NameAegis == aegisName, ct);
    }

    public async Task<List<MobEntity>> GetByLevelRangeAsync(ushort minLevel, ushort maxLevel, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => m.Level >= minLevel && m.Level <= maxLevel)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetByRaceAsync(string race, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => m.Race == race)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetByElementAsync(string element, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => m.Element == element)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetBySizeAsync(string size, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => m.Size == size)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetAllMvpsAsync(CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => m.ModeMvp == 1)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => EF.Functions.Like(m.NameEnglish, $"%{searchTerm}%"))
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetByDropItemAsync(string itemName, CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .Where(m => 
                m.Drop1Item == itemName || m.Drop2Item == itemName || 
                m.Drop3Item == itemName || m.Drop4Item == itemName ||
                m.Drop5Item == itemName || m.Drop6Item == itemName ||
                m.Drop7Item == itemName || m.Drop8Item == itemName ||
                m.Drop9Item == itemName || m.Drop10Item == itemName ||
                m.Mvpdrop1Item == itemName || m.Mvpdrop2Item == itemName ||
                m.Mvpdrop3Item == itemName)
            .ToListAsync(ct);
    }

    public async Task<List<MobEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.MobDb
            .AsNoTracking()
            .ToListAsync(ct);
    }
}