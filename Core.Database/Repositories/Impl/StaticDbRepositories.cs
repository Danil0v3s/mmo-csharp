using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Microsoft.EntityFrameworkCore;

namespace Core.Database.Repositories.Impl;

internal sealed class AbraDbRepository(GameDbContext ctx) : IAbraDbRepository
{
    public async Task<IReadOnlyList<AbraDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AbraDb.AsNoTracking().ToListAsync(ct);
}

internal sealed class MagicMushroomDbRepository(GameDbContext ctx) : IMagicMushroomDbRepository
{
    public async Task<IReadOnlyList<MagicMushroomDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MagicMushroomDb.AsNoTracking().ToListAsync(ct);
}

internal sealed class SpellbookDbRepository(GameDbContext ctx) : ISpellbookDbRepository
{
    public async Task<IReadOnlyList<SpellbookDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.SpellbookDb.AsNoTracking().ToListAsync(ct);
    public async Task<SpellbookDbEntity?> GetByBookAsync(string bookNameAegis, CancellationToken ct = default)
        => await ctx.SpellbookDb.AsNoTracking().FirstOrDefaultAsync(s => s.BookNameAegis == bookNameAegis, ct);
}

internal sealed class QuestDbRepository(GameDbContext ctx) : IQuestDbRepository
{
    public async Task<IReadOnlyList<QuestDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.QuestDb.AsNoTracking().ToListAsync(ct);
    public async Task<QuestDbEntity?> GetByIdAsync(uint questId, CancellationToken ct = default)
        => await ctx.QuestDb.AsNoTracking().FirstOrDefaultAsync(q => q.QuestId == questId, ct);
}

internal sealed class PetDbRepository(GameDbContext ctx) : IPetDbRepository
{
    public async Task<IReadOnlyList<PetDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.PetDb.AsNoTracking().ToListAsync(ct);
    public async Task<PetDbEntity?> GetByMobAsync(string mobAegis, CancellationToken ct = default)
        => await ctx.PetDb.AsNoTracking().FirstOrDefaultAsync(p => p.MobAegis == mobAegis, ct);
}

internal sealed class AchievementDbRepository(GameDbContext ctx) : IAchievementDbRepository
{
    public async Task<IReadOnlyList<AchievementDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.AchievementDb.AsNoTracking().ToListAsync(ct);
    public async Task<AchievementDbEntity?> GetByIdAsync(uint achievementId, CancellationToken ct = default)
        => await ctx.AchievementDb.AsNoTracking().FirstOrDefaultAsync(a => a.AchievementId == achievementId, ct);
}

internal sealed class HomunculusDbRepository(GameDbContext ctx) : IHomunculusDbRepository
{
    public async Task<IReadOnlyList<HomunculusDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.HomunculusDb.AsNoTracking().ToListAsync(ct);
    public async Task<HomunculusDbEntity?> GetByClassAsync(string classAegis, CancellationToken ct = default)
        => await ctx.HomunculusDb.AsNoTracking().FirstOrDefaultAsync(h => h.ClassAegis == classAegis, ct);
}

internal sealed class MercenaryDbRepository(GameDbContext ctx) : IMercenaryDbRepository
{
    public async Task<IReadOnlyList<MercenaryDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MercenaryDb.AsNoTracking().ToListAsync(ct);
    public async Task<MercenaryDbEntity?> GetByIdAsync(uint mercId, CancellationToken ct = default)
        => await ctx.MercenaryDb.AsNoTracking().FirstOrDefaultAsync(m => m.MercId == mercId, ct);
}

internal sealed class InstanceDbRepository(GameDbContext ctx) : IInstanceDbRepository
{
    public async Task<IReadOnlyList<InstanceDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.InstanceDb.AsNoTracking().ToListAsync(ct);
    public async Task<InstanceDbEntity?> GetByIdAsync(uint instanceId, CancellationToken ct = default)
        => await ctx.InstanceDb.AsNoTracking().FirstOrDefaultAsync(i => i.InstanceId == instanceId, ct);
}

internal sealed class MercenarySkillDbRepository(GameDbContext ctx) : IMercenarySkillDbRepository
{
    public async Task<IReadOnlyList<MercenarySkillDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.MercenarySkillDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<MercenarySkillDbEntity>> GetByMercAsync(uint mercId, CancellationToken ct = default)
        => await ctx.MercenarySkillDb.AsNoTracking().Where(s => s.MercId == mercId).ToListAsync(ct);
}

internal sealed class HomunculusSkillTreeDbRepository(GameDbContext ctx) : IHomunculusSkillTreeDbRepository
{
    public async Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetAllAsync(CancellationToken ct = default)
        => await ctx.HomunculusSkillTreeDb.AsNoTracking().ToListAsync(ct);
    public async Task<IReadOnlyList<HomunculusSkillTreeDbEntity>> GetByClassAsync(string classAegis, CancellationToken ct = default)
        => await ctx.HomunculusSkillTreeDb.AsNoTracking().Where(s => s.ClassAegis == classAegis).ToListAsync(ct);
}

// Battleground catalog: read via existing JSON-payload table; DB-8 wires
// the deserializer. No new typed repo here.
