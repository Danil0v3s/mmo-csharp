using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement;

/// <summary>
/// First-slice <c>pc_setpos</c> implementation. Sequence mirrors
/// rAthena pc.cpp:6949:
///
/// 1. Validate target map / cell.
/// 2. Stop walking and attacking (rAthena: <c>unit_stop_walking</c> +
///    <c>unit_stop_attack</c>).
/// 3. Notify the old AOI we vanished (CLR_TELEPORT for same-map,
///    CLR_OUTSIGHT for cross-map).
/// 4. Re-bind the entity to the new map/cell.
/// 5. Tell the client where it is now (<c>ZC_NPCACK_MAPMOVE</c>).
/// 6. Wait — the client requests the new map's load; once it sends
///    <c>CZ_NOTIFY_ACTORINIT</c> the normal LoadEndAck path re-broadcasts
///    the entity into the new AOI.
///
/// Cross-server warps (multi-map-server setups) need a char-server
/// auth_node re-handoff before step 5 — that's a separate slice.
/// </summary>
public sealed class PcSetposService : IPcSetposService
{
    private readonly IMapWorldRegistry _world;
    private readonly IEntityRegistry _entities;
    private readonly IVisibilityService _visibility;
    private readonly IAttackService _attack;
    private readonly Status.ISessionManagerAccessor _sessions;
    private readonly ILogger<PcSetposService> _logger;

    public PcSetposService(
        IMapWorldRegistry world,
        IEntityRegistry entities,
        IVisibilityService visibility,
        IAttackService attack,
        Status.ISessionManagerAccessor sessions,
        ILogger<PcSetposService> logger)
    {
        _world = world;
        _entities = entities;
        _visibility = visibility;
        _attack = attack;
        _sessions = sessions;
        _logger = logger;
    }

    public SetposResult Setpos(PlayerEntity pc, string mapName, short x, short y)
    {
        var map = _world.Get(mapName);
        if (map == null) return SetposResult.UnknownMap;
        if (x < 0 || y < 0 || x >= map.Xs || y >= map.Ys) return SetposResult.OffMap;
        if (!map.IsWalkable(x, y))
        {
            // rAthena: pc_setpos tries adjacent cells via map_search_freecell.
            // Trim to a 3x3 sweep around the requested cell for the first slice.
            if (!TryFindNearbyWalkable(map, x, y, out x, out y))
                return SetposResult.NotWalkable;
        }

        // Stop combat and movement before relocation — rAthena uses
        // unit_stop_walking(USW_FIXPOS) here.
        _attack.StopAttack(pc);
        pc.Walk = null;

        var newMapId = (uint)mapName.GetHashCode();
        var sameMap = pc.MapId == newMapId;

        // Vanish on current AOI before mutating position so the broadcast
        // hits the correct viewers (TELEPORT for same-map, OUTSIGHT for cross).
        _visibility.NotifyVanishedToArea(pc, sameMap ? VanishReason.Teleport : VanishReason.Outsight);

        // Re-bind to new cell. Same-map: spatial-index move. Cross-map:
        // remove + re-add so the per-map bucket flip happens correctly.
        if (sameMap)
        {
            pc.SetPosition(newMapId, x, y);
            _entities.Move(pc.Id, x, y);
        }
        else
        {
            _entities.Remove(pc.Id);
            pc.SetPosition(newMapId, x, y);
            _entities.Add(pc);
        }

        var session = _sessions.GetByEntityId(pc.Id);
        if (session != null)
        {
            // ZC_NPCACK_MAPMOVE — client uses this to switch its tile cache
            // and re-request its full surroundings via CZ_NOTIFY_ACTORINIT.
            session.EnqueuePacket(new ZC_NPCACK_MAPMOVE
            {
                MapName = mapName,
                X = x,
                Y = y,
            });
            // Reset the session's spawn coords so the next LoadEndAck spawns
            // us at the destination (NotifyActorInitHandler reads SpawnX/Y).
            session.SpawnX = x;
            session.SpawnY = y;
            session.MapId = newMapId;
        }

        if (sameMap)
        {
            // For same-map teleport the client doesn't fire another
            // LoadEndAck — broadcast STANDENTRY ourselves so other PCs
            // see the new position.
            _visibility.NotifySpawnedToArea(pc);
            _visibility.SendCurrentViewToSelf(pc);
        }

        _logger.LogInformation(
            "pc_setpos: char {Char} → {Map} ({X},{Y}) [sameMap={Same}]",
            pc.CharacterId, mapName, x, y, sameMap);
        return SetposResult.Ok;
    }

    private static bool TryFindNearbyWalkable(MapData map, short cx, short cy, out short outX, out short outY)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var nx = (short)(cx + dx);
                var ny = (short)(cy + dy);
                if (nx < 0 || ny < 0 || nx >= map.Xs || ny >= map.Ys) continue;
                if (map.IsWalkable(nx, ny))
                {
                    outX = nx; outY = ny;
                    return true;
                }
            }
        }
        outX = cx; outY = cy;
        return false;
    }
}
