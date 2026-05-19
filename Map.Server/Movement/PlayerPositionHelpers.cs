using Map.Server.Entities;
using Map.Server.Status;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement;

/// <summary>
/// Default <see cref="IPlayerPositionHelpers"/>. All four helpers
/// have real implementations:
///
/// <list type="bullet">
///   <item><c>IsLastpointSpecial</c> — hardcoded list of rAthena's
///         special-savepoint maps (sec_pri, mvp_room, instances).</item>
///   <item><c>RandomWarp</c> — bounded retry on
///         <see cref="MapData.IsWalkable"/> + Setpos.</item>
///   <item><c>Memo</c> — writes to <see cref="PlayerEntity.MemoPoints"/>.</item>
///   <item><c>IsBasilicaCell</c> — checks the player's SC list for
///         SC_BASILICA (the SC is applied while standing on the
///         Basilica unit cell).</item>
/// </list>
/// </summary>
public sealed class PlayerPositionHelpers : IPlayerPositionHelpers
{
    private static readonly Random Rng = new();

    /// <summary>
    /// rAthena's special-savepoint map list (pc.cpp:1058
    /// pc_lastpoint_special). These maps refuse the "remember as
    /// last save point" behavior — using @save on them is a no-op.
    /// </summary>
    private static readonly HashSet<string> SpecialMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        "sec_pri",         // jail
        "queen_room",      // MVP rooms
        "lasa_dun_q",
        "lasa_dun01",
        "lasa_dun02",
        "lasa_dun03",
        // Add instance map names as they port.
    };

    private readonly IMapWorldRegistry _maps;
    private readonly IPcSetposService _setpos;
    private readonly IStatusChangeService _scs;
    private readonly ILogger<PlayerPositionHelpers> _logger;

    public PlayerPositionHelpers(
        IMapWorldRegistry maps,
        IPcSetposService setpos,
        IStatusChangeService scs,
        ILogger<PlayerPositionHelpers> logger)
    {
        _maps = maps;
        _setpos = setpos;
        _scs = scs;
        _logger = logger;
    }

    public bool IsLastpointSpecial(string mapName)
        => !string.IsNullOrEmpty(mapName) && SpecialMaps.Contains(mapName);

    public bool RandomWarp(PlayerEntity pc)
    {
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == pc.MapId);
        if (map == null) return false;
        for (var i = 0; i < 200; i++)
        {
            var x = (short)Rng.Next(0, map.Xs);
            var y = (short)Rng.Next(0, map.Ys);
            if (!map.IsWalkable(x, y)) continue;
            _setpos.Setpos(pc, map.Name, x, y);
            return true;
        }
        return false;
    }

    public bool Memo(PlayerEntity pc, int slot)
    {
        // rAthena Warp Portal memo — 3 slots; slot < 0 picks the
        // first empty / oldest. We use the same convention.
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == pc.MapId);
        if (map == null) return false;
        var idx = slot;
        if (idx < 0 || idx >= pc.MemoPoints.Length)
        {
            // Pick first empty; if all full, fall back to slot 0.
            idx = 0;
            for (var i = 0; i < pc.MemoPoints.Length; i++)
            {
                if (string.IsNullOrEmpty(pc.MemoPoints[i].MapName)) { idx = i; break; }
            }
        }
        pc.MemoPoints[idx] = (map.Name, pc.X, pc.Y);
        _logger.LogInformation(
            "pc_memo: char {Char} slot {Slot} = {Map} ({X},{Y})",
            pc.CharacterId, idx, map.Name, pc.X, pc.Y);
        return true;
    }

    public bool IsBasilicaCell(PlayerEntity pc)
    {
        // rAthena Basilica / Land Protector / Pneuma all apply an SC
        // to the player while standing on the unit cell. Checking the
        // SC list is the canonical proxy — the unit-on-place handler
        // applies SC_BASILICA on enter, removes on leave.
        if (_scs.Get(pc, StatusType.Basilica) != null) return true;
        return false;
    }
}
