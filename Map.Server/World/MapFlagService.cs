using Map.Server.Scripting;

namespace Map.Server.World;

/// <summary>
/// Default <see cref="IMapFlagService"/>. Reads
/// <c>INpcRegistry.AllMapFlags()</c> once at first query and caches
/// the result in a per-map flag bitmask. The registry never mutates
/// after script load, so the cache is build-once / read-many.
/// </summary>
public sealed class MapFlagService : IMapFlagService
{
    private readonly INpcRegistry _registry;
    private readonly object _gate = new();
    private Dictionary<string, MapFlagBits>? _byMap;

    public MapFlagService(INpcRegistry registry)
    {
        _registry = registry;
    }

    public bool IsSet(string mapName, MapFlag flag)
    {
        if (string.IsNullOrEmpty(mapName)) return false;
        var snapshot = EnsureSnapshot();
        return snapshot.TryGetValue(mapName, out var bits) && bits.Has(flag);
    }

    /// <summary>
    /// Mutate the cached bitmask. Used by GM commands (@pvpon, @pvpoff,
    /// @gvgon, @gvgoff) — rAthena flips the per-map flag in-place at
    /// runtime. Auto-creates the map entry if it isn't tracked yet.
    /// </summary>
    public void Set(string mapName, MapFlag flag, bool on)
    {
        if (string.IsNullOrEmpty(mapName)) return;
        var snapshot = EnsureSnapshot();
        lock (_gate)
        {
            if (!snapshot.TryGetValue(mapName, out var bits))
            {
                bits = new MapFlagBits();
                snapshot[mapName] = bits;
            }
            if (on) bits.Set(flag);
            else bits.Clear(flag);
        }
    }

    private Dictionary<string, MapFlagBits> EnsureSnapshot()
    {
        var snapshot = _byMap;
        if (snapshot != null) return snapshot;

        lock (_gate)
        {
            if (_byMap != null) return _byMap;
            var built = new Dictionary<string, MapFlagBits>(StringComparer.OrdinalIgnoreCase);
            foreach (var reg in _registry.AllMapFlags())
            {
                if (!TryParseFlag(reg.Flag, out var flag)) continue;
                if (!built.TryGetValue(reg.Map, out var bits))
                {
                    bits = new MapFlagBits();
                    built[reg.Map] = bits;
                }
                bits.Set(flag);
            }
            _byMap = built;
            return _byMap;
        }
    }

    private static bool TryParseFlag(string raw, out MapFlag flag)
    {
        // rAthena script flag tokens are lowercase no-underscore-or-dashes
        // strings. We accept the script token verbatim plus our enum form.
        flag = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "nopvp": flag = MapFlag.NoPvp; return true;
            case "noskill": flag = MapFlag.NoSkill; return true;
            case "noteleport": flag = MapFlag.NoTeleport; return true;
            case "nodrop": flag = MapFlag.NoDrop; return true;
            case "noloot": flag = MapFlag.NoLoot; return true;
            case "noexp": flag = MapFlag.NoExp; return true;
            case "nopenalty": flag = MapFlag.NoPenalty; return true;
            case "nosave": flag = MapFlag.NoSave; return true;
            case "notrade": flag = MapFlag.NoTrade; return true;
            case "nochat": flag = MapFlag.NoChat; return true;
            case "novending": flag = MapFlag.NoVending; return true;
            case "nobuyingstore": flag = MapFlag.NoBuyingStore; return true;
            case "nobranch": flag = MapFlag.NoBranch; return true;
            case "nomemo": flag = MapFlag.NoMemo; return true;
            case "gvg": flag = MapFlag.Gvg; return true;
            default: return false;
        }
    }

    private sealed class MapFlagBits
    {
        private int _bits;
        public void Set(MapFlag flag) => _bits |= 1 << (int)flag;
        public void Clear(MapFlag flag) => _bits &= ~(1 << (int)flag);
        public bool Has(MapFlag flag) => (_bits & (1 << (int)flag)) != 0;
    }
}
