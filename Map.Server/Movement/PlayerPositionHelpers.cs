using Map.Server.Entities;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement;

/// <summary>
/// Default <see cref="IPlayerPositionHelpers"/>.
///
/// <c>pc_randomwarp</c> is implemented via a bounded retry on
/// <see cref="MapData.IsWalkable"/>; matches rAthena's
/// map_search_freecell bounded scan.
///
/// The other helpers are documented stubs — Basilica cell filtering
/// needs the ground-unit registry; memo needs warp scroll item
/// scripts; lastpoint_special needs the special-map list.
/// </summary>
public sealed class PlayerPositionHelpers : IPlayerPositionHelpers
{
    private static readonly Random Rng = new();
    private readonly IMapWorldRegistry _maps;
    private readonly IPcSetposService _setpos;
    private readonly ILogger<PlayerPositionHelpers> _logger;

    public PlayerPositionHelpers(IMapWorldRegistry maps, IPcSetposService setpos, ILogger<PlayerPositionHelpers> logger)
    {
        _maps = maps;
        _setpos = setpos;
        _logger = logger;
    }

    public bool IsLastpointSpecial(string mapName)
    {
        // rAthena hard-codes a list (sec_pri jail, izlude_d, etc.).
        // First slice returns false for everything — the caller can
        // still apply pc_setsavepoint freely. Replace with config when
        // the special-map list ports.
        return false;
    }

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
        // Warp Portal memo — 3 slots in rAthena (mmo_charstatus.memo_point).
        // Persistence column missing on our CharacterData proto today;
        // log and return success so script callers don't break.
        _logger.LogDebug("pc_memo stub: char {Char} slot {Slot}", pc.CharacterId, slot);
        return true;
    }

    public bool IsBasilicaCell(PlayerEntity pc)
    {
        // Ground-unit cell effects (Basilica / Land Protector) not
        // ported yet — return false so the caller assumes any cell is
        // attackable.
        return false;
    }
}
