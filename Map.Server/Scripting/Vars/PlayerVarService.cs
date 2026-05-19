using System.Collections.Concurrent;
using Core.Database.Context;
using Core.Database.Entities;
using Map.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Map.Server.Scripting.Vars;

/// <summary>
/// In-memory cache + DB-backed <see cref="IPlayerVarService"/>.
///
/// rAthena writes through to MySQL on every <c>pc_setregistry</c> call;
/// for the C# port we mirror that semantic via a tracked-dirty set that
/// the autosave / logout flush walks. Reads are O(1) lookup against the
/// hashmap once the session loaded.
/// </summary>
public sealed class PlayerVarService : IPlayerVarService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PlayerVarService> _logger;

    // (charId, scope, key, index) → value. Scope=GlobalAccount/Account
    // store under account id; scope=Char/CharTemp under char id. We
    // key on charId uniformly for cache simplicity; the persistence
    // layer maps to the right table by scope.
    private readonly ConcurrentDictionary<(int CharId, VarScope Scope, string Key, uint Index), long> _num = new();
    private readonly ConcurrentDictionary<(int CharId, VarScope Scope, string Key, uint Index), string> _str = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<(VarScope Scope, string Key, uint Index), bool>> _dirtyNum = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<(VarScope Scope, string Key, uint Index), bool>> _dirtyStr = new();

    public PlayerVarService(IServiceProvider services, ILogger<PlayerVarService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public long ReadNum(PlayerEntity pc, VarScope scope, string name, uint index = 0)
        => _num.TryGetValue((pc.CharacterId, scope, name, index), out var v) ? v : 0;

    public string ReadStr(PlayerEntity pc, VarScope scope, string name, uint index = 0)
        => _str.TryGetValue((pc.CharacterId, scope, name, index), out var v) ? v : string.Empty;

    public void WriteNum(PlayerEntity pc, VarScope scope, string name, long value, uint index = 0)
    {
        var key = (pc.CharacterId, scope, name, index);
        if (value == 0) _num.TryRemove(key, out _);
        else _num[key] = value;
        if (scope != VarScope.CharTemp) MarkDirty(_dirtyNum, pc.CharacterId, (scope, name, index));
    }

    public void WriteStr(PlayerEntity pc, VarScope scope, string name, string value, uint index = 0)
    {
        var key = (pc.CharacterId, scope, name, index);
        if (string.IsNullOrEmpty(value)) _str.TryRemove(key, out _);
        else _str[key] = value;
        if (scope != VarScope.CharTemp) MarkDirty(_dirtyStr, pc.CharacterId, (scope, name, index));
    }

    public async Task LoadAsync(PlayerEntity pc, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        // char_reg_num / char_reg_str — keyed by char id.
        await foreach (var r in db.Set<CharRegNumEntity>().Where(r => r.CharId == pc.CharacterId).AsAsyncEnumerable().WithCancellation(ct))
            _num[(pc.CharacterId, VarScope.Char, r.Key, r.Index)] = r.Value;
        await foreach (var r in db.Set<CharRegStrEntity>().Where(r => r.CharId == pc.CharacterId).AsAsyncEnumerable().WithCancellation(ct))
            _str[(pc.CharacterId, VarScope.Char, r.Key, r.Index)] = r.Value;

        // acc_reg_num / acc_reg_str — keyed by account id; cached under
        // char id for read simplicity (one PC per session).
        await foreach (var r in db.Set<AccRegNumEntity>().Where(r => r.AccountId == pc.AccountId).AsAsyncEnumerable().WithCancellation(ct))
            _num[(pc.CharacterId, VarScope.Account, r.Key, r.Index)] = r.Value;
        await foreach (var r in db.Set<AccRegStrEntity>().Where(r => r.AccountId == pc.AccountId).AsAsyncEnumerable().WithCancellation(ct))
            _str[(pc.CharacterId, VarScope.Account, r.Key, r.Index)] = r.Value;

        // global_acc_reg_num / global_acc_reg_str — keyed by account.
        await foreach (var r in db.Set<GlobalAccRegNumEntity>().Where(r => r.AccountId == pc.AccountId).AsAsyncEnumerable().WithCancellation(ct))
            _num[(pc.CharacterId, VarScope.GlobalAccount, r.Key, r.Index)] = r.Value;
        await foreach (var r in db.Set<GlobalAccRegStrEntity>().Where(r => r.AccountId == pc.AccountId).AsAsyncEnumerable().WithCancellation(ct))
            _str[(pc.CharacterId, VarScope.GlobalAccount, r.Key, r.Index)] = r.Value;
    }

    public async Task FlushAsync(PlayerEntity pc, CancellationToken ct)
    {
        if (!_dirtyNum.TryGetValue(pc.CharacterId, out var dn) && !_dirtyStr.TryGetValue(pc.CharacterId, out _))
            return;
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        if (_dirtyNum.TryGetValue(pc.CharacterId, out var dirtyNum))
        {
            foreach (var (k, _) in dirtyNum.ToArray())
            {
                await UpsertNumAsync(db, pc, k.Scope, k.Key, k.Index, ct);
                dirtyNum.TryRemove(k, out _);
            }
        }
        if (_dirtyStr.TryGetValue(pc.CharacterId, out var dirtyStr))
        {
            foreach (var (k, _) in dirtyStr.ToArray())
            {
                await UpsertStrAsync(db, pc, k.Scope, k.Key, k.Index, ct);
                dirtyStr.TryRemove(k, out _);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertNumAsync(GameDbContext db, PlayerEntity pc, VarScope scope, string key, uint index, CancellationToken ct)
    {
        var value = ReadNum(pc, scope, key, index);
        switch (scope)
        {
            case VarScope.Char:
                {
                    var row = await db.Set<CharRegNumEntity>()
                        .FirstOrDefaultAsync(r => r.CharId == pc.CharacterId && r.Key == key && r.Index == index, ct);
                    if (value == 0)
                    {
                        if (row != null) db.Set<CharRegNumEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<CharRegNumEntity>().Add(new() { CharId = pc.CharacterId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
            case VarScope.Account:
                {
                    var row = await db.Set<AccRegNumEntity>()
                        .FirstOrDefaultAsync(r => r.AccountId == pc.AccountId && r.Key == key && r.Index == index, ct);
                    if (value == 0)
                    {
                        if (row != null) db.Set<AccRegNumEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<AccRegNumEntity>().Add(new() { AccountId = pc.AccountId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
            case VarScope.GlobalAccount:
                {
                    var row = await db.Set<GlobalAccRegNumEntity>()
                        .FirstOrDefaultAsync(r => r.AccountId == pc.AccountId && r.Key == key && r.Index == index, ct);
                    if (value == 0)
                    {
                        if (row != null) db.Set<GlobalAccRegNumEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<GlobalAccRegNumEntity>().Add(new() { AccountId = pc.AccountId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
        }
    }

    private async Task UpsertStrAsync(GameDbContext db, PlayerEntity pc, VarScope scope, string key, uint index, CancellationToken ct)
    {
        var value = ReadStr(pc, scope, key, index);
        switch (scope)
        {
            case VarScope.Char:
                {
                    var row = await db.Set<CharRegStrEntity>()
                        .FirstOrDefaultAsync(r => r.CharId == pc.CharacterId && r.Key == key && r.Index == index, ct);
                    if (string.IsNullOrEmpty(value))
                    {
                        if (row != null) db.Set<CharRegStrEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<CharRegStrEntity>().Add(new() { CharId = pc.CharacterId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
            case VarScope.Account:
                {
                    var row = await db.Set<AccRegStrEntity>()
                        .FirstOrDefaultAsync(r => r.AccountId == pc.AccountId && r.Key == key && r.Index == index, ct);
                    if (string.IsNullOrEmpty(value))
                    {
                        if (row != null) db.Set<AccRegStrEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<AccRegStrEntity>().Add(new() { AccountId = pc.AccountId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
            case VarScope.GlobalAccount:
                {
                    var row = await db.Set<GlobalAccRegStrEntity>()
                        .FirstOrDefaultAsync(r => r.AccountId == pc.AccountId && r.Key == key && r.Index == index, ct);
                    if (string.IsNullOrEmpty(value))
                    {
                        if (row != null) db.Set<GlobalAccRegStrEntity>().Remove(row);
                    }
                    else if (row == null)
                        db.Set<GlobalAccRegStrEntity>().Add(new() { AccountId = pc.AccountId, Key = key, Index = index, Value = value });
                    else
                        row.Value = value;
                    break;
                }
        }
    }

    private static void MarkDirty<TKey>(
        ConcurrentDictionary<int, ConcurrentDictionary<TKey, bool>> store,
        int charId, TKey key) where TKey : notnull
    {
        var per = store.GetOrAdd(charId, _ => new ConcurrentDictionary<TKey, bool>());
        per[key] = true;
    }
}
