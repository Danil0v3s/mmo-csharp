using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class HotkeyRepository : IHotkeyRepository
{
    private readonly GameDbContext _context;

    public HotkeyRepository(GameDbContext context) => _context = context;

    public async Task<IReadOnlyList<HotkeyEntity>> GetByCharIdAsync(int charId, CancellationToken ct = default)
        => await _context.Hotkeys.AsNoTracking()
            .Where(h => h.CharId == charId)
            .ToListAsync(ct);

    public async Task ReplaceAsync(int charId, IEnumerable<HotkeyEntity> hotkeys, CancellationToken ct = default)
    {
        // rAthena pattern (char.cpp:mmo_char_tosql): DELETE all rows for
        // this char, then INSERT each non-empty slot. Type == 0 means
        // "empty slot" — rAthena skips persisting those, so we do too.
        var existing = await _context.Hotkeys
            .Where(h => h.CharId == charId)
            .ToListAsync(ct);
        if (existing.Count > 0) _context.Hotkeys.RemoveRange(existing);

        foreach (var hk in hotkeys)
        {
            if (hk.Type == 0) continue;
            _context.Hotkeys.Add(new HotkeyEntity
            {
                CharId = charId,
                Hotkey = hk.Hotkey,
                Type = hk.Type,
                ItemSkillId = hk.ItemSkillId,
                SkillLvl = hk.SkillLvl,
            });
        }
        await _context.SaveChangesAsync(ct);
    }
}
