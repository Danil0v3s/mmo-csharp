using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Pathing;
using Map.Server.Visibility;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement.UnitOps;

/// <summary>Default <see cref="IUnitOpsService"/>. Shells forward to MovementService / AttackService when wired.</summary>
public sealed class UnitOpsService : IUnitOpsService
{
    private readonly IPathService _path;
    private readonly IEntityRegistry _entities;
    private readonly IVisibilityService _visibility;
    private readonly ILogger<UnitOpsService> _logger;

    // rAthena unit.cpp:52 — direction → cell delta lookup. Layout matches
    // the enum (0=N, 1=NW, 2=W, 3=SW, 4=S, 5=SE, 6=E, 7=NE).
    //   const int16 dirx[DIR_MAX] = { 0,-1,-1,-1, 0, 1, 1, 1 };
    //   const int16 diry[DIR_MAX] = { 1, 1, 0,-1,-1,-1, 0, 1 };
    private static readonly short[] DirX = { 0, -1, -1, -1, 0, 1, 1, 1 };
    private static readonly short[] DirY = { 1, 1, 0, -1, -1, -1, 0, 1 };

    public UnitOpsService(
        IPathService path,
        IEntityRegistry entities,
        IVisibilityService visibility,
        ILogger<UnitOpsService> logger)
    {
        _path = path;
        _entities = entities;
        _visibility = visibility;
        _logger = logger;
    }

    public int Warp(Entity bl, string mapName, short x, short y, byte flag) => 0;
    public bool WalkToXy(Entity bl, short x, short y, byte flag) => false;
    public bool WalkToBl(Entity bl, Entity target, short range, byte flag) => false;
    public bool StopWalking(Entity bl, byte type) => false;
    public bool StopAttack(Entity bl) => false;
    public bool CanMove(Entity bl) => true;
    public bool Attack(Entity src, EntityId targetId, byte continuous) => false;

    /// <summary>
    /// rAthena <c>unit_blown</c> wrapper that takes a direction +
    /// count. Pushes <paramref name="bl"/> <paramref name="count"/>
    /// cells along <paramref name="direction"/>, stopping at the
    /// first non-walkable cell (mirrors <c>path_blownpos</c>'s halt-
    /// at-wall behavior).
    ///
    /// <para>Sends the standard rAthena knockback packet pair to the
    /// entity's AOI: <see cref="ZC_HIGHJUMP"/> for the slide visual
    /// then <see cref="ZC_STOPMOVE"/> for the authoritative endpoint.
    /// Mirrors <c>clif_blown</c> which is <c>clif_slide</c> +
    /// <c>clif_fixpos</c>.</para>
    ///
    /// <para>Returns true iff the entity actually moved. A count of 0,
    /// or a direction that immediately hits a wall, yields false
    /// without emitting packets.</para>
    /// </summary>
    public bool BlownBy(Entity bl, int direction, int count)
    {
        if (count <= 0) return false;
        if (direction < 0 || direction >= 8)
        {
            _logger.LogWarning("BlownBy: invalid direction {Direction} on entity {EntityId}", direction, bl.Id.Value);
            return false;
        }

        // Translate direction → (dx, dy). rAthena: dx = dirx[dir]; dy = diry[dir].
        // The dirx/diry tables encode the unit step per cardinal+diagonal direction.
        var dx = DirX[direction];
        var dy = DirY[direction];

        // rAthena path_blownpos: walk count cells in (dx, dy), stopping
        // when the next cell would be non-walkable. Wrapped behind the
        // IPathService for the C# port so map-cell knowledge stays on
        // the pathing service.
        var (nx, ny) = _path.BlownPos(bl.MapId, bl.X, bl.Y, direction, count);

        // No movement (we were already against a wall in that direction).
        if (nx == bl.X && ny == bl.Y) return false;

        // Reposition in the spatial index. EntityRegistry.Move keeps the
        // entity on the same map (rAthena unit_blown handles cross-map
        // via unit_warp — out of scope for knockback).
        _entities.Move(bl.Id, nx, ny);

        // rAthena clif_blown = clif_slide + clif_fixpos. Both go to AOI:
        //   clif_slide visualizes the displacement (ZC_HIGHJUMP),
        //   clif_fixpos locks the endpoint cell (ZC_STOPMOVE).
        var slide = new ZC_HIGHJUMP { SrcId = bl.Id.Value, X = nx, Y = ny };
        var fix = new ZC_STOPMOVE { EntityId = bl.Id.Value, X = nx, Y = ny };
        _visibility.SendToArea(bl, slide);
        _visibility.SendToArea(bl, fix);

        return true;
    }

    public bool SetDir(Entity bl, byte dir) => false;
    public byte GetDir(Entity bl) => 0;
    public bool MovePos(Entity bl, short x, short y, byte easy, bool checkColl) => false;
    public bool SkillUseId(Entity src, EntityId targetId, ushort skillId, ushort skillLevel) => false;
    public bool SkillUsePos(Entity src, short x, short y, ushort skillId, ushort skillLevel) => false;
    public int RemoveMap(Entity bl, byte clearType) => 0;
    public int Free(Entity bl, byte clearType) => 0;
    public int ChangeViewSize(Entity bl, short size) => 0;
    public void DataCreate(Entity bl) { }
    public bool CanReachBl(Entity src, Entity target, short range) => true;
    public bool CanReachPos(Entity src, short x, short y, byte easy) => true;
}
