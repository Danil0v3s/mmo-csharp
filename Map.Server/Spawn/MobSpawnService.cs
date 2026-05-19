using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Items;
using Map.Server.Mob;
using Map.Server.Movement;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Spawn;

public sealed class MobSpawnService : IMobSpawnService, IMobDeathSink
{
    private const int WanderRange = 7;             // cells (matches plan default)
    private const int WanderMinDelayMs = 5_000;
    private const int WanderMaxDelayMs = 30_000;
    private const int MaxPlacementAttempts = 32;   // before giving up on a spawn position

    private readonly IMobSpawnRegistry _registry;
    private readonly IEntityRegistry _entities;
    private readonly IMapWorldRegistry _worldRegistry;
    private readonly IMobDb _mobDb;
    private readonly IItemCatalog _itemCatalog;
    private readonly IItemDropService _itemDrops;
    private readonly IMovementService _movement;
    private readonly IVisibilityService _visibility;
    private readonly EntityIdAllocator _idAllocator;
    private readonly IStatusCalcService _statusCalc;
    private readonly ILogger<MobSpawnService> _logger;
    private readonly Random _rng;

    // entry-id ↔ live count, used to honor Amount on initial + respawn passes
    private readonly Dictionary<MobSpawnEntry, int> _liveCounts = new();
    private readonly List<PendingRespawn> _pendingRespawns = new();
    private readonly object _gate = new();

    public MobSpawnService(
        IMobSpawnRegistry registry,
        IEntityRegistry entities,
        IMapWorldRegistry worldRegistry,
        IMobDb mobDb,
        IItemCatalog itemCatalog,
        IItemDropService itemDrops,
        IMovementService movement,
        IVisibilityService visibility,
        EntityIdAllocator idAllocator,
        IStatusCalcService statusCalc,
        ILogger<MobSpawnService> logger,
        Random? rng = null)
    {
        _registry = registry;
        _entities = entities;
        _worldRegistry = worldRegistry;
        _mobDb = mobDb;
        _itemCatalog = itemCatalog;
        _itemDrops = itemDrops;
        _movement = movement;
        _visibility = visibility;
        _idAllocator = idAllocator;
        _statusCalc = statusCalc;
        _logger = logger;
        _rng = rng ?? Random.Shared;
    }

    public int PendingRespawnCount
    {
        get { lock (_gate) return _pendingRespawns.Count; }
    }

    public Entity? SpawnAt(string mapName, int classId, short x, short y)
    {
        var map = _worldRegistry.All.FirstOrDefault(m => string.Equals(m.Name, mapName, StringComparison.OrdinalIgnoreCase));
        if (map == null) return null;
        var dbEntry = _mobDb.Get(classId);
        if (dbEntry == null) return null;
        if (!map.IsWalkable(x, y))
        {
            // Try a small ring around the requested cell — mirrors rAthena
            // map_search_freecell behavior when @monster lands on a wall.
            var found = false;
            for (var r = 1; r <= 3 && !found; r++)
            for (var dy = -r; dy <= r && !found; dy++)
            for (var dx = -r; dx <= r && !found; dx++)
            {
                var nx = (short)(x + dx);
                var ny = (short)(y + dy);
                if (nx < 0 || ny < 0 || nx >= map.Xs || ny >= map.Ys) continue;
                if (!map.IsWalkable(nx, ny)) continue;
                x = nx; y = ny; found = true;
            }
            if (!found) return null;
        }

        var mapId = (uint)map.Name.GetHashCode();
        // Synthetic spawn entry — RespawnDelayMs = 0 keeps it from
        // re-spawning after death (one-shot GM summon).
        var entry = new MobSpawnEntry
        {
            MobClassId = classId, MapId = mapId, X = x, Y = y,
            Amount = 1, RespawnDelayMs = 0, RespawnJitterMs = 0,
        };
        var mob = new MobEntity(_idAllocator.NextMob(), dbEntry, entry, mapId, x, y);
        _statusCalc.CalcMob(mob);
        _entities.Add(mob);
        _visibility.NotifySpawnedToArea(mob);
        return mob;
    }

    public void SpawnInitial()
    {
        foreach (var entry in _registry.All())
        {
            var map = ResolveMap(entry.MapId);
            if (map == null)
            {
                _logger.LogWarning(
                    "Spawn entry {ClassId} targets unknown map 0x{MapId:X8} — skipped",
                    entry.MobClassId, entry.MapId);
                continue;
            }
            var dbEntry = _mobDb.Get(entry.MobClassId);
            if (dbEntry == null)
            {
                _logger.LogWarning(
                    "Spawn entry references unknown mob class {ClassId} — skipped",
                    entry.MobClassId);
                continue;
            }

            int current;
            lock (_gate) { _liveCounts.TryGetValue(entry, out current); }
            for (var i = current; i < entry.Amount; i++)
            {
                if (!TrySpawnOne(entry, dbEntry, map))
                {
                    _logger.LogWarning(
                        "Couldn't place mob {ClassId} on {Map} after {Attempts} attempts",
                        entry.MobClassId, map.Name, MaxPlacementAttempts);
                }
            }
        }
    }

    public void Tick()
    {
        var now = Environment.TickCount64;

        // Respawn pass — promote pending entries whose deadline has elapsed.
        List<PendingRespawn> due;
        lock (_gate)
        {
            due = _pendingRespawns.Where(r => r.At <= now).ToList();
            if (due.Count > 0)
            {
                _pendingRespawns.RemoveAll(r => r.At <= now);
            }
        }
        foreach (var pending in due)
        {
            var map = ResolveMap(pending.Entry.MapId);
            var dbEntry = _mobDb.Get(pending.Entry.MobClassId);
            if (map == null || dbEntry == null) continue;
            TrySpawnOne(pending.Entry, dbEntry, map);
        }

        // Wander pass — every idle mob whose wander cooldown elapsed picks a
        // new cell and walks there. Cheap O(N) scan; mobs are O(thousands)
        // in worst case, no spatial indexing needed yet.
        foreach (var entity in _entities.All())
        {
            if (entity is not MobEntity mob) continue;
            if (mob.Walk != null) continue;
            if (mob.NextWanderTick > now) continue;

            var map = ResolveMap(mob.MapId);
            if (map == null) continue;

            if (TryPickWanderTarget(map, mob, out var tx, out var ty))
            {
                _movement.TryStartWalk(mob, tx, ty);
            }
            mob.NextWanderTick = now + _rng.Next(WanderMinDelayMs, WanderMaxDelayMs + 1);
        }
    }

    public bool KillMob(EntityId id) => KillMob(id, lastHitter: null);

    public bool KillMob(EntityId id, PlayerEntity? lastHitter)
    {
        var entity = _entities.Get(id);
        if (entity is not MobEntity mob) return false;

        // Roll the mob's drop table BEFORE removing it so drop coordinates
        // align with the corpse cell. ItemDropService broadcasts each fall
        // entry independently; the order doesn't matter to clients.
        RollAndDropLoot(mob, lastHitter);

        _visibility.NotifyVanishedToArea(mob, VanishReason.Died);
        _entities.Remove(id);

        if (mob.Origin != null)
        {
            lock (_gate)
            {
                if (_liveCounts.TryGetValue(mob.Origin, out var n))
                {
                    _liveCounts[mob.Origin] = Math.Max(0, n - 1);
                }
                var jitter = mob.Origin.RespawnJitterMs > 0
                    ? _rng.Next(0, mob.Origin.RespawnJitterMs + 1)
                    : 0;
                _pendingRespawns.Add(new PendingRespawn(
                    mob.Origin,
                    Environment.TickCount64 + mob.Origin.RespawnDelayMs + jitter));
            }
        }
        return true;
    }

    // ---- helpers ----

    /// <summary>
    /// Iterate the mob's drop table; each entry has an independent
    /// <c>rng &lt; rate/10000</c> roll. Survivors get spawned as floor items
    /// at the mob's death cell with a random sub-cell offset (rAthena's
    /// visual jitter so multiple drops don't overlap exactly). MVP-only
    /// drops, exp distribution, party share, and rate modifiers all live
    /// with the broader combat work in <c>adjacent/combat.md</c>.
    /// </summary>
    private void RollAndDropLoot(MobEntity mob, PlayerEntity? lastHitter)
    {
        if (mob.DbEntry == null || mob.DbEntry.Drops.Count == 0) return;

        foreach (var drop in mob.DbEntry.Drops)
        {
            if (drop.Rate <= 0) continue;
            if (_rng.Next(10_000) >= drop.Rate) continue;

            var itemRow = _itemCatalog.GetByAegisName(drop.Item);
            if (itemRow == null)
            {
                _logger.LogWarning(
                    "Mob {Class} drops unknown item '{Item}' (no row in item_db)",
                    mob.ClassId, drop.Item);
                continue;
            }

            _itemDrops.DropOnFloor(
                mob.MapId, mob.X, mob.Y,
                itemId: (int)itemRow.Id,
                amount: 1,
                subX: (byte)_rng.Next(0, 16),
                subY: (byte)_rng.Next(0, 16),
                // Last-hitter gets exclusive pickup priority — matches
                // rAthena's first_charid attribution (mob.cpp:mob_dead).
                ownerCharId: lastHitter?.CharacterId ?? 0,
                ownerPartyId: lastHitter?.PartyId ?? 0);
        }
    }

    private bool TrySpawnOne(MobSpawnEntry entry, MobDbEntry dbEntry, MapData map)
    {
        if (!TryPickSpawnCell(entry, map, out var x, out var y)) return false;

        var mob = new MobEntity(_idAllocator.NextMob(), dbEntry, entry, entry.MapId, x, y);
        // Renewal stat hydration — mirrors status_calc_mob_ at mob_spawn
        // (status.cpp:2731). Must run before the spawn broadcast so any
        // observer reading max_hp / level on the wire sees the right values.
        _statusCalc.CalcMob(mob);
        _entities.Add(mob);

        lock (_gate)
        {
            _liveCounts[entry] = _liveCounts.GetValueOrDefault(entry) + 1;
        }
        _visibility.NotifySpawnedToArea(mob);
        return true;
    }

    private bool TryPickSpawnCell(MobSpawnEntry entry, MapData map, out short x, out short y)
    {
        var anywhere = entry.X == 0 && entry.Y == 0 && entry.Xs == 0 && entry.Ys == 0;
        for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            short cx, cy;
            if (anywhere)
            {
                cx = (short)_rng.Next(map.Xs);
                cy = (short)_rng.Next(map.Ys);
            }
            else
            {
                cx = (short)(entry.X + _rng.Next(-entry.Xs, entry.Xs + 1));
                cy = (short)(entry.Y + _rng.Next(-entry.Ys, entry.Ys + 1));
            }
            if (map.IsWalkable(cx, cy))
            {
                x = cx;
                y = cy;
                return true;
            }
        }
        x = y = 0;
        return false;
    }

    private bool TryPickWanderTarget(MapData map, MobEntity mob, out short x, out short y)
    {
        for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            var cx = (short)(mob.X + _rng.Next(-WanderRange, WanderRange + 1));
            var cy = (short)(mob.Y + _rng.Next(-WanderRange, WanderRange + 1));
            if ((cx == mob.X && cy == mob.Y) || !map.IsWalkable(cx, cy)) continue;
            x = cx;
            y = cy;
            return true;
        }
        x = y = 0;
        return false;
    }

    private MapData? ResolveMap(uint mapId)
    {
        foreach (var map in _worldRegistry.All)
        {
            if ((uint)map.Name.GetHashCode() == mapId) return map;
        }
        return null;
    }

    private readonly record struct PendingRespawn(MobSpawnEntry Entry, long At);
}
