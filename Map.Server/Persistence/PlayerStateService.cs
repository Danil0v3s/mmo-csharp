using Core.Database.Context;
using Core.Database.Entities;
using Core.Database.Repositories.Api;
using Map.Server.Entities;
using Map.Server.Session;
using Microsoft.EntityFrameworkCore;

namespace Map.Server.Persistence;

public sealed class PlayerStateService : IPlayerStateService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IEntityRegistry _entities;
    private readonly ILogger<PlayerStateService> _logger;

    public PlayerStateService(
        IServiceScopeFactory scopes,
        IEntityRegistry entities,
        ILogger<PlayerStateService> logger)
    {
        _scopes = scopes;
        _entities = entities;
        _logger = logger;
    }

    public async Task LoadAsync(MapSessionData session, CancellationToken ct = default)
    {
        if (session.CharacterId is not { } charId || session.AccountId is not { } accountId)
        {
            session.VarRegs = PlayerVarRegs.Empty();
            return;
        }

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        var permNum = await db.CharacterRegistersNum.AsNoTracking()
            .Where(r => r.CharId == charId).ToListAsync(ct);
        var permStr = await db.CharacterRegistersStr.AsNoTracking()
            .Where(r => r.CharId == charId).ToListAsync(ct);
        var acctNum = await db.AccountRegistersNum.AsNoTracking()
            .Where(r => r.AccountId == accountId).ToListAsync(ct);
        var acctStr = await db.AccountRegistersStr.AsNoTracking()
            .Where(r => r.AccountId == accountId).ToListAsync(ct);
        var globNum = await db.GlobalAccountRegistersNum.AsNoTracking()
            .Where(r => r.AccountId == accountId).ToListAsync(ct);
        var globStr = await db.GlobalAccountRegistersStr.AsNoTracking()
            .Where(r => r.AccountId == accountId).ToListAsync(ct);

        session.VarRegs = new PlayerVarRegs(
            BuildScope(permNum.Select(r => (r.Key, (object)r.Value)),
                       permStr.Select(r => (r.Key, (object)r.Value))),
            BuildScope(acctNum.Select(r => (r.Key, (object)r.Value)),
                       acctStr.Select(r => (r.Key, (object)r.Value))),
            BuildScope(globNum.Select(r => (r.Key, (object)r.Value)),
                       globStr.Select(r => (r.Key, (object)r.Value))));

        _logger.LogDebug(
            "Loaded var-regs for char {CharId} (acc {AccountId}): perm {Perm}, account {Account}, accountGlobal {Global}",
            charId, accountId,
            permNum.Count + permStr.Count,
            acctNum.Count + acctStr.Count,
            globNum.Count + globStr.Count);
    }

    public async Task SaveAsync(MapSessionData session, bool finalSave, CancellationToken ct = default)
    {
        if (session.CharacterId is not { } charId || session.AccountId is not { } accountId)
        {
            return;
        }

        using var scope = _scopes.CreateScope();
        var charRepo = scope.ServiceProvider.GetRequiredService<ICharacterRepository>();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        await SaveCoreStateAsync(session, charId, charRepo, ct);

        if (session.VarRegs is { } regs)
        {
            await SavePermAsync(regs.Perm, charId, db, ct);
            await SaveAccountAsync(regs.Account, accountId, db, ct);
            await SaveAccountGlobalAsync(regs.AccountGlobal, accountId, db, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    // ----- core state -----

    private async Task SaveCoreStateAsync(MapSessionData session, int charId, ICharacterRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(charId, ct);
        if (entity == null) return;

        var data = session.CharacterData;
        if (data != null)
        {
            entity.Zeny = data.Zeny;
            entity.BaseLevel = (ushort)data.BaseLevel;
            entity.JobLevel = (ushort)data.JobLevel;
            entity.BaseExp = data.BaseExp;
            entity.JobExp = data.JobExp;
            entity.Str = (ushort)data.Str;
            entity.Agi = (ushort)data.Agi;
            entity.Vit = (ushort)data.Vit;
            entity.Int = (ushort)data.IntStat;
            entity.Dex = (ushort)data.Dex;
            entity.Luk = (ushort)data.Luk;
        }

        if (session.EntityId is { } eid && _entities.Get(eid) is PlayerEntity pc)
        {
            entity.Hp = (uint)Math.Max(0, pc.Hp);
            entity.MaxHp = (uint)Math.Max(1, pc.MaxHp);
            entity.Sp = (uint)Math.Max(0, pc.Sp);
            entity.MaxSp = (uint)Math.Max(1, pc.MaxSp);
        }

        await repo.UpdateAsync(entity, ct);
    }

    // ----- per-scope save loops -----

    private static async Task SavePermAsync(PlayerVarScope scope, int charId, GameDbContext db, CancellationToken ct)
    {
        foreach (var (key, value) in EnumerateChanges(scope))
        {
            if (value is long numValue)
            {
                var row = await db.CharacterRegistersNum.FirstOrDefaultAsync(
                    r => r.CharId == charId && r.Key == key, ct);
                if (row != null) row.Value = numValue;
                else db.CharacterRegistersNum.Add(new CharRegNumEntity { CharId = charId, Key = key, Value = numValue });
            }
            else if (value is string strValue)
            {
                var row = await db.CharacterRegistersStr.FirstOrDefaultAsync(
                    r => r.CharId == charId && r.Key == key, ct);
                if (row != null) row.Value = strValue;
                else db.CharacterRegistersStr.Add(new CharRegStrEntity { CharId = charId, Key = key, Value = strValue });
            }
        }
    }

    private static async Task SaveAccountAsync(PlayerVarScope scope, int accountId, GameDbContext db, CancellationToken ct)
    {
        foreach (var (key, value) in EnumerateChanges(scope))
        {
            if (value is long numValue)
            {
                var row = await db.AccountRegistersNum.FirstOrDefaultAsync(
                    r => r.AccountId == accountId && r.Key == key, ct);
                if (row != null) row.Value = numValue;
                else db.AccountRegistersNum.Add(new AccRegNumEntity { AccountId = accountId, Key = key, Value = numValue });
            }
            else if (value is string strValue)
            {
                var row = await db.AccountRegistersStr.FirstOrDefaultAsync(
                    r => r.AccountId == accountId && r.Key == key, ct);
                if (row != null) row.Value = strValue;
                else db.AccountRegistersStr.Add(new AccRegStrEntity { AccountId = accountId, Key = key, Value = strValue });
            }
        }
    }

    private static async Task SaveAccountGlobalAsync(PlayerVarScope scope, int accountId, GameDbContext db, CancellationToken ct)
    {
        foreach (var (key, value) in EnumerateChanges(scope))
        {
            if (value is long numValue)
            {
                var row = await db.GlobalAccountRegistersNum.FirstOrDefaultAsync(
                    r => r.AccountId == accountId && r.Key == key, ct);
                if (row != null) row.Value = numValue;
                else db.GlobalAccountRegistersNum.Add(new GlobalAccRegNumEntity { AccountId = accountId, Key = key, Value = numValue });
            }
            else if (value is string strValue)
            {
                var row = await db.GlobalAccountRegistersStr.FirstOrDefaultAsync(
                    r => r.AccountId == accountId && r.Key == key, ct);
                if (row != null) row.Value = strValue;
                else db.GlobalAccountRegistersStr.Add(new GlobalAccRegStrEntity { AccountId = accountId, Key = key, Value = strValue });
            }
        }
    }

    // ----- diff helpers -----

    /// <summary>
    /// Walk the bag's current state; yield (key, coercedValue) pairs for
    /// entries that are new or differ from the loaded snapshot. Values
    /// are coerced to <see cref="long"/> or <see cref="string"/> so the
    /// scope savers don't have to deal with JS-side types.
    /// </summary>
    private static IEnumerable<(string Key, object Value)> EnumerateChanges(PlayerVarScope scope)
    {
        foreach (var key in scope.Bag.Keys.ToList())
        {
            var raw = scope.Bag[key];
            if (raw is null) continue;
            object coerced = raw switch
            {
                long l => l,
                int i => (long)i,
                short s => (long)s,
                byte b => (long)b,
                uint u => (long)u,
                double d when !double.IsNaN(d) && d == Math.Truncate(d) => (long)d,
                double d => d.ToString("R"),
                string s => s,
                bool b => (long)(b ? 1 : 0),
                _ => raw.ToString() ?? string.Empty,
            };
            if (scope.Loaded.TryGetValue(key, out var orig) && Equals(orig, coerced))
                continue;
            yield return (key, coerced);
        }
    }

    private static PlayerVarScope BuildScope(
        IEnumerable<(string Key, object Value)> nums,
        IEnumerable<(string Key, object Value)> strs)
    {
        var dict = new Dictionary<string, object>();
        foreach (var (k, v) in nums) dict[k] = v;
        // String rows override num rows on key collision (shouldn't happen in
        // practice; either table is the source of truth for a given key).
        foreach (var (k, v) in strs) dict[k] = v;
        return new PlayerVarScope(dict);
    }
}
