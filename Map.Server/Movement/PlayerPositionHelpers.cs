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
    private readonly IMapFlagService? _mapFlags;

    public PlayerPositionHelpers(
        IMapWorldRegistry maps,
        IPcSetposService setpos,
        IStatusChangeService scs,
        ILogger<PlayerPositionHelpers> logger,
        IMapFlagService? mapFlags = null)
    {
        _maps = maps;
        _setpos = setpos;
        _scs = scs;
        _logger = logger;
        _mapFlags = mapFlags;
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
        // COMBAT-67 — rAthena pc_memo (pc.cpp:7098). `slot == -1` is the client
        // "remember this point" path: dedup the current map, memmove the list down,
        // and insert at slot 0. A fixed `slot` writes that slot directly.
        var map = _maps.All.FirstOrDefault(m => (uint)m.Name.GetHashCode() == pc.MapId);
        if (map == null) return false;

        // Mapflag gate: MF_NOMEMO / MF_NOWARPTO forbid memo-ing here.
        if (_mapFlags != null
            && (_mapFlags.IsSet(map.Name, MapFlag.NoMemo) || _mapFlags.IsSet(map.Name, MapFlag.NoWarpTo)))
            return false;

        // Input gate (rAthena: pos < -1 || pos >= MAX_MEMOPOINTS).
        if (slot < -1 || slot >= pc.MemoPoints.Length) return false;

        // AL_WARP level gate: skill < 1 → not learned; skill < 2 || skill-2 < pos → low level.
        var lv = pc.LearnedSkills.GetValueOrDefault(SkillIdAlWarp);
        if (lv < 1) return false;
        if (lv < 2 || lv - 2 < slot) return false;

        if (slot == -1)
        {
            // Dedup: find the first slot already holding the current map (else = length).
            var dupAt = pc.MemoPoints.Length;
            for (var i = 0; i < pc.MemoPoints.Length; i++)
                if (string.Equals(pc.MemoPoints[i].MapName, map.Name, StringComparison.OrdinalIgnoreCase))
                { dupAt = i; break; }
            // memmove [0 .. shift-1] → [1 .. shift] (insert-at-0), shift = min(dupAt, MAX-1).
            var shift = Math.Min(dupAt, pc.MemoPoints.Length - 1);
            for (var i = shift; i > 0; i--)
                pc.MemoPoints[i] = pc.MemoPoints[i - 1];
            slot = 0;
        }

        pc.MemoPoints[slot] = (map.Name, pc.X, pc.Y);
        _logger.LogInformation(
            "pc_memo: char {Char} slot {Slot} = {Map} ({X},{Y})",
            pc.CharacterId, slot, map.Name, pc.X, pc.Y);
        return true;
    }

    // AL_WARP skill id (Map.Server.Skills.SkillIds.AL_WARP = 27) — referenced here without
    // a Map.Server.Skills using to keep the movement layer dependency-light.
    private const ushort SkillIdAlWarp = 27;

    public bool IsBasilicaCell(PlayerEntity pc)
    {
        // rAthena Basilica / Land Protector / Pneuma all apply an SC
        // to the player while standing on the unit cell. Checking the
        // SC list is the canonical proxy — the unit-on-place handler
        // applies SC_BASILICA_CELL on enter, removes on leave.
        // (`StatusType.Basilica` is the original Priest skill SC; the
        // *cell* variant is `BasilicaCell`, per rAthena status.hpp.)
        if (_scs.Get(pc, StatusType.BasilicaCell) != null) return true;
        return false;
    }
}
