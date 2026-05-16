namespace Map.Server.Entities;

/// <summary>
/// Per-map bucketed cell-grid spatial index. Mirrors rAthena's
/// <c>block_list_head</c> table in map.cpp — each cell holds a list of
/// entities at that exact cell, and range queries scan the (2·range+1)²
/// bounding box.
///
/// Buckets are stored as a flat row-major array of <see cref="HashSet{T}"/>
/// indexed by <c>y * Xs + x</c>. HashSet gives O(1) insert/remove and
/// duplicate-protection for the rare case of double-add (defensive — should
/// never happen but harmless if it does).
///
/// Thread safety: a single coarse lock guards both insertions and queries.
/// Single-threaded gameplay on the tick keeps contention near zero;
/// gRPC-driven add/remove (EnterMap / LeaveMap) competes only briefly.
/// </summary>
public sealed class MapSpatialIndex
{
    private readonly object _gate = new();
    private readonly HashSet<EntityId>?[] _buckets;
    private readonly short _xs;
    private readonly short _ys;

    public MapSpatialIndex(short xs, short ys)
    {
        if (xs <= 0 || ys <= 0) throw new ArgumentException($"Invalid map size {xs}x{ys}");
        _xs = xs;
        _ys = ys;
        _buckets = new HashSet<EntityId>?[xs * ys];
    }

    public short Xs => _xs;
    public short Ys => _ys;

    public void Insert(EntityId id, short x, short y)
    {
        if ((uint)x >= (uint)_xs || (uint)y >= (uint)_ys) return;
        lock (_gate)
        {
            ref var bucket = ref _buckets[y * _xs + x];
            bucket ??= new HashSet<EntityId>();
            bucket.Add(id);
        }
    }

    public void Remove(EntityId id, short x, short y)
    {
        if ((uint)x >= (uint)_xs || (uint)y >= (uint)_ys) return;
        lock (_gate)
        {
            var bucket = _buckets[y * _xs + x];
            bucket?.Remove(id);
        }
    }

    public void Move(EntityId id, short fromX, short fromY, short toX, short toY)
    {
        if (fromX == toX && fromY == toY) return;
        lock (_gate)
        {
            if ((uint)fromX < (uint)_xs && (uint)fromY < (uint)_ys)
            {
                _buckets[fromY * _xs + fromX]?.Remove(id);
            }
            if ((uint)toX < (uint)_xs && (uint)toY < (uint)_ys)
            {
                ref var bucket = ref _buckets[toY * _xs + toX];
                bucket ??= new HashSet<EntityId>();
                bucket.Add(id);
            }
        }
    }

    /// <summary>
    /// Collect every entity id in cells within ±range of (cx, cy). Inclusive
    /// on both axes; bounded by map edges. Returns a snapshot copy — safe to
    /// iterate while the registry mutates.
    /// </summary>
    public List<EntityId> ForEachInRange(short cx, short cy, short range)
    {
        var x0 = (short)Math.Max(0, cx - range);
        var x1 = (short)Math.Min(_xs - 1, cx + range);
        var y0 = (short)Math.Max(0, cy - range);
        var y1 = (short)Math.Min(_ys - 1, cy + range);
        var results = new List<EntityId>();
        lock (_gate)
        {
            for (var y = y0; y <= y1; y++)
            {
                var rowOff = y * _xs;
                for (var x = x0; x <= x1; x++)
                {
                    var bucket = _buckets[rowOff + x];
                    if (bucket == null) continue;
                    foreach (var id in bucket) results.Add(id);
                }
            }
        }
        return results;
    }

    /// <summary>
    /// Collect every entity id inside the rectangle [x0, x1] × [y0, y1]
    /// (inclusive, automatically clamped to map bounds).
    /// </summary>
    public List<EntityId> ForEachInArea(short x0, short y0, short x1, short y1)
    {
        if (x0 > x1) (x0, x1) = (x1, x0);
        if (y0 > y1) (y0, y1) = (y1, y0);
        if (x0 < 0) x0 = 0;
        if (y0 < 0) y0 = 0;
        if (x1 > _xs - 1) x1 = (short)(_xs - 1);
        if (y1 > _ys - 1) y1 = (short)(_ys - 1);
        var results = new List<EntityId>();
        lock (_gate)
        {
            for (var y = y0; y <= y1; y++)
            {
                var rowOff = y * _xs;
                for (var x = x0; x <= x1; x++)
                {
                    var bucket = _buckets[rowOff + x];
                    if (bucket == null) continue;
                    foreach (var id in bucket) results.Add(id);
                }
            }
        }
        return results;
    }
}
