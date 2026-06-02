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

        // COMBAT-48 — Warp Portal memo points (rAthena memo_point[3]). Ordered
        // by row id so the persisted save order survives the round trip; the
        // first 3 rows fill the slots, extras (shouldn't exist) are ignored.
        var memos = await db.Memos.AsNoTracking()
            .Where(m => m.CharId == charId)
            .OrderBy(m => m.MemoId).ToListAsync(ct);
        var memoSlots = new (string, short, short)[3];
        for (var i = 0; i < memoSlots.Length && i < memos.Count; i++)
            memoSlots[i] = (memos[i].Map, (short)memos[i].X, (short)memos[i].Y);
        session.MemoPoints = memoSlots;

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
        }

        var pendingInventoryInserts = await SaveInventoryAsync(session, charId, db, ct);

        await SaveMemoPointsAsync(session, charId, db, ct);

        // Single SaveChanges covers var-regs + inventory + memo: any mid-flight
        // failure rolls back consistently, and we cut one round trip.
        await db.SaveChangesAsync(ct);

        // Copy auto-increment Ids back onto the runtime objects so the
        // next SaveAsync treats them as UPDATEs instead of duplicate
        // INSERTs.
        foreach (var (runtime, entity) in pendingInventoryInserts)
        {
            runtime.Id = entity.Id;
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

        // Live runtime values from the PlayerEntity win over the snapshot
        // from session.CharacterData when they exist — pc_gainexp,
        // pc_checkbaselevelup, status-point grants on level-up etc. mutate
        // the entity, not the snapshot, so without this the autosave would
        // persist stale level/exp values.
        if (session.EntityId is { } eid && _entities.Get(eid) is PlayerEntity pc)
        {
            entity.Hp = (uint)Math.Max(0, pc.Hp);
            entity.MaxHp = (uint)Math.Max(1, pc.MaxHp);
            entity.Sp = (uint)Math.Max(0, pc.Sp);
            entity.MaxSp = (uint)Math.Max(1, pc.MaxSp);
            entity.BaseLevel = (ushort)Math.Max(1, pc.Level);
            entity.JobLevel = (ushort)Math.Max(1, pc.JobLevel);
            entity.BaseExp = (ulong)Math.Max(0, pc.BaseExp);
            entity.JobExp = (ulong)Math.Max(0, pc.JobExp);
            entity.StatusPoint = (uint)Math.Max(0, pc.StatusPoints);
            entity.SkillPoint = (uint)Math.Max(0, pc.SkillPoints);
            // Per-stat runtime values land here once equip + buff stripping
            // are wired — for now session.CharacterData carries the saved
            // base stats unchanged (no /stat allocation packet yet).
            entity.LastX = (ushort)pc.X;
            entity.LastY = (ushort)pc.Y;
        }

        await repo.UpdateAsync(entity, ct);
    }

    // ----- inventory -----

    /// <summary>
    /// Persist the session's runtime inventory back to the DB. Strategy:
    /// <list type="bullet">
    ///   <item>Items with <c>Id == 0</c> are new — <c>INSERT</c> and copy
    ///     the generated Id back onto the runtime <c>InventoryItem</c>.</item>
    ///   <item>Items with <c>Id != 0</c> are existing rows — <c>UPDATE</c>
    ///     amount + equip + refine + grade + attribute. Cards / options /
    ///     bound flags are immutable per slot today.</item>
    ///   <item>Ids in <see cref="MapSessionData.RemovedInventoryIds"/> are
    ///     <c>DELETE</c>d. Used by future delItem / consumeItem paths.</item>
    /// </list>
    /// Mirrors rAthena's <c>chmapif_parse_savechar</c> inventory block,
    /// which the char-server fans out into per-row upserts.
    /// </summary>
    private static async Task<List<(Map.Server.Inventory.InventoryItem Runtime, InventoryEntity Entity)>> SaveInventoryAsync(
        MapSessionData session, int charId, GameDbContext db, CancellationToken ct)
    {
        var pendingInserts = new List<(Map.Server.Inventory.InventoryItem Runtime, InventoryEntity Entity)>();
        if (session.Inventory is not { } items) return pendingInserts;

        // DELETEs first so a re-insert at the same slot index doesn't
        // collide on any unique constraint we add later.
        if (session.RemovedInventoryIds.Count > 0)
        {
            var ids = session.RemovedInventoryIds.ToArray();
            var rows = await db.Inventories
                .Where(r => r.CharId == charId && ids.Contains(r.Id))
                .ToListAsync(ct);
            db.Inventories.RemoveRange(rows);
            session.RemovedInventoryIds.Clear();
        }

        // Index existing rows so we don't roundtrip per item on UPDATE.
        var existingIds = items.Where(i => i.Id > 0).Select(i => i.Id).ToArray();
        var existing = existingIds.Length > 0
            ? await db.Inventories.Where(r => existingIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct)
            : new Dictionary<int, InventoryEntity>();

        foreach (var item in items)
        {
            if (item.Id == 0)
            {
                var entity = MapToEntity(charId, item);
                db.Inventories.Add(entity);
                pendingInserts.Add((item, entity));
            }
            else if (existing.TryGetValue(item.Id, out var entity))
            {
                // Mutable fields. Cards / options / bound stay as loaded.
                entity.Amount = item.Amount;
                entity.Equip = item.Equip;
                entity.Identify = (short)(item.Identified ? 1 : 0);
                entity.Refine = item.Refine;
                entity.Attribute = item.Attribute;
                entity.Favorite = item.Favorite;
                entity.EquipSwitch = item.EquipSwitch;
                entity.EnchantGrade = item.EnchantGrade;
            }
        }

        return pendingInserts;
    }

    // ----- memo points (Warp Portal) -----

    /// <summary>
    /// COMBAT-48 — persist the live <see cref="PlayerEntity.MemoPoints"/> back to
    /// the <c>memo</c> table (rAthena <c>memo_point[3]</c>). The runtime entity is
    /// the source of truth (pc_memo mutates it); fall back to the load-time
    /// snapshot when the entity is gone (final save after despawn). Strategy:
    /// drop the char's rows and re-insert the non-empty slots in order, so the
    /// row order matches the in-memory slot order on the next load.
    /// </summary>
    private async Task SaveMemoPointsAsync(MapSessionData session, int charId, GameDbContext db, CancellationToken ct)
    {
        var memos = session.EntityId is { } eid && _entities.Get(eid) is PlayerEntity pc
            ? pc.MemoPoints
            : session.MemoPoints;
        if (memos == null) return;

        var existing = await db.Memos.Where(m => m.CharId == charId).ToListAsync(ct);
        db.Memos.RemoveRange(existing);
        foreach (var (map, x, y) in memos)
        {
            if (string.IsNullOrEmpty(map)) continue;
            db.Memos.Add(new MemoEntity { CharId = charId, Map = map, X = (ushort)x, Y = (ushort)y });
        }
    }

    private static InventoryEntity MapToEntity(int charId, Map.Server.Inventory.InventoryItem item)
    {
        var opts = item.Options;
        InventoryEntity e = new()
        {
            CharId = charId,
            NameId = item.NameId,
            Amount = item.Amount,
            Equip = item.Equip,
            Identify = (short)(item.Identified ? 1 : 0),
            Refine = item.Refine,
            Attribute = item.Attribute,
            Card0 = item.Card0,
            Card1 = item.Card1,
            Card2 = item.Card2,
            Card3 = item.Card3,
            ExpireTime = item.ExpireTime,
            Favorite = item.Favorite,
            Bound = item.Bound,
            UniqueId = item.UniqueId,
            EquipSwitch = item.EquipSwitch,
            EnchantGrade = item.EnchantGrade,
        };
        if (opts.Length > 0) { e.OptionId0 = opts[0].Id; e.OptionVal0 = opts[0].Value; e.OptionParm0 = opts[0].Param; }
        if (opts.Length > 1) { e.OptionId1 = opts[1].Id; e.OptionVal1 = opts[1].Value; e.OptionParm1 = opts[1].Param; }
        if (opts.Length > 2) { e.OptionId2 = opts[2].Id; e.OptionVal2 = opts[2].Value; e.OptionParm2 = opts[2].Param; }
        if (opts.Length > 3) { e.OptionId3 = opts[3].Id; e.OptionVal3 = opts[3].Value; e.OptionParm3 = opts[3].Param; }
        if (opts.Length > 4) { e.OptionId4 = opts[4].Id; e.OptionVal4 = opts[4].Value; e.OptionParm4 = opts[4].Param; }
        return e;
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
