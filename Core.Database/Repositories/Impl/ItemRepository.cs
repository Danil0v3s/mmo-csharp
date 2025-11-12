using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class ItemRepository : IItemRepository
{
    private readonly GameDbContext _context;

    public ItemRepository(GameDbContext context)
    {
        _context = context;
    }

    public async Task<ItemEntity?> GetByIdAsync(uint itemId, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);
    }

    public async Task<ItemEntity?> GetByAegisNameAsync(string aegisName, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.NameAegis == aegisName, ct);
    }

    public async Task<List<ItemEntity>> GetByTypeAsync(string type, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .Where(i => i.Type == type)
            .ToListAsync(ct);
    }

    public async Task<List<ItemEntity>> GetByTypeAndSubtypeAsync(string type, string subtype, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .Where(i => i.Type == type && i.Subtype == subtype)
            .ToListAsync(ct);
    }

    public async Task<List<ItemEntity>> SearchByNameAsync(string searchTerm, int limit = 50, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .Where(i => EF.Functions.Like(i.NameEnglish, $"%{searchTerm}%"))
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<ItemEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<ItemEntity>> GetByPriceRangeAsync(uint minPrice, uint maxPrice, CancellationToken ct = default)
    {
        return await _context.ItemDb
            .AsNoTracking()
            .Where(i => i.PriceBuy >= minPrice && i.PriceBuy <= maxPrice)
            .ToListAsync(ct);
    }

    public async Task<List<ItemEntity>> GetByEquipLocationAsync(string location, CancellationToken ct = default)
    {
        // This is simplified - you might want to check multiple location fields
        return await _context.ItemDb
            .AsNoTracking()
            .Where(i => 
                (location == "HEAD_TOP" && i.LocationHeadTop == 1) ||
                (location == "ARMOR" && i.LocationArmor == 1) ||
                (location == "WEAPON" && i.LocationRightHand == 1))
            .ToListAsync(ct);
    }
}