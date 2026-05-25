using Core.Server.Packets.Out.ZC;
using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Pathing;
using Map.Server.Skills;
using Map.Server.Status;
using Map.Server.Visibility;
using Map.Server.World;
using Microsoft.Extensions.Logging;

namespace Map.Server.Movement.UnitOps;

/// <summary>
/// Default <see cref="IUnitOpsService"/>. Mirrors rAthena
/// <c>unit.cpp</c> public surface (unit_walktoxy / unit_attack /
/// unit_stop_walking / unit_warp / …) by delegating to the
/// already-canonical owners — <see cref="IMovementService"/>,
/// <see cref="IAttackService"/>, <see cref="ISkillCastService"/>,
/// <see cref="IPathService"/>, <see cref="IWarpDispatcher"/>.
/// <para>The shell stays a thin façade so skill plugins, atcommands,
/// and AI engines have one rAthena-named entry point per public
/// function in unit.cpp.</para>
/// </summary>
public sealed class UnitOpsService : IUnitOpsService
{
    private readonly IPathService _path;
    private readonly IEntityRegistry _entities;
    private readonly IVisibilityService _visibility;
    private readonly IMovementService _movement;
    private readonly IAttackService _attack;
    private readonly IServiceProvider? _services; // lazy ISkillCastService (cycle).
    private readonly IMapWorldRegistry? _world;
    private readonly Warps.IWarpDispatcher? _warpDispatcher;
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
        IMovementService movement,
        IAttackService attack,
        ILogger<UnitOpsService> logger,
        IMapWorldRegistry? world = null,
        Warps.IWarpDispatcher? warpDispatcher = null,
        IServiceProvider? services = null)
    {
        _path = path;
        _entities = entities;
        _visibility = visibility;
        _movement = movement;
        _attack = attack;
        _world = world;
        _warpDispatcher = warpDispatcher;
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// rAthena <c>unit_warp</c> (unit.cpp:1693) — cross-map teleport.
    /// For PCs the warp executes via <see cref="Warps.IWarpDispatcher"/>
    /// (full pc_setpos cascade with map-change packet); for mobs the
    /// path is a direct registry move when the same map, or no-op when
    /// cross-map (rAthena mobs warp only by skill — out of scope here).
    /// Returns 0 on success, non-zero on validation failure.
    /// </summary>
    public int Warp(Entity bl, string mapName, short x, short y, byte flag)
    {
        if (bl is PlayerEntity pc && _warpDispatcher != null && _world != null)
        {
            var map = _world.Get(mapName);
            if (map == null) return 1; // unknown map
            // Synthesize a one-shot WarpRegistration so the dispatcher
            // doesn't need a new method shape; the rAthena pc_setpos
            // cascade only reads ToMap/ToX/ToY anyway.
            var warp = new Scripting.Records.WarpRegistration
            {
                FromMap = "",
                FromX = 0, FromY = 0, AreaXs = 0, AreaYs = 0,
                ToMap = mapName, ToX = x, ToY = y,
                Type = "warp",
            };
            _warpDispatcher.OnEnterWarp(pc, warp);
            return 0;
        }
        // Mob path — same map only.
        if (_world?.Get(mapName) is { } mobMap && (uint)mobMap.Name.GetHashCode() == bl.MapId)
        {
            if (!mobMap.IsWalkable(x, y)) return 1;
            _entities.Move(bl.Id, x, y);
            var fix = new ZC_STOPMOVE { EntityId = bl.Id.Value, X = x, Y = y };
            _visibility.SendToArea(bl, fix);
            return 0;
        }
        _logger.LogDebug(
            "unit_warp: cross-map mob warp ({Entity} → {Map}) not supported",
            bl.Id.Value, mapName);
        return 1;
    }

    /// <summary>rAthena <c>unit_walktoxy</c> — schedule a walk.</summary>
    public bool WalkToXy(Entity bl, short x, short y, byte flag)
        => _movement.TryStartWalk(bl, x, y);

    /// <summary>
    /// rAthena <c>unit_walktobl</c> — walk toward target's cell.
    /// Stops <paramref name="range"/> cells short when range &gt; 0 so
    /// the caller can use this as a "close the gap" helper (mob AI).
    /// </summary>
    public bool WalkToBl(Entity bl, Entity target, short range, byte flag)
    {
        if (target == null) return false;
        if (target.MapId != bl.MapId) return false;
        // Step toward the target — for range=0 we walk straight to the
        // target cell; for range>0 we stop adjacent. The path service's
        // closest-cell variant isn't exposed yet, so we approximate by
        // walking to the target's cell and letting the AttackService /
        // skill range check handle the stop.
        return _movement.TryStartWalk(bl, target.X, target.Y);
    }

    /// <summary>
    /// rAthena <c>unit_stop_walking</c>. Type bits (rAthena
    /// USW_FIXPOS / FORCESTOP / MOVE_ONCE / RELEASE_TARGET) collapse to
    /// "cancel walk now" — the C# walk state has no per-step fixpos
    /// emission, so the broader rAthena type-bit dispatch reduces to
    /// CancelWalk.
    /// </summary>
    public bool StopWalking(Entity bl, byte type)
    {
        var wasWalking = bl.Walk != null;
        _movement.CancelWalk(bl);
        return wasWalking;
    }

    /// <summary>rAthena <c>unit_stop_attack</c> — cancel any in-flight auto-attack.</summary>
    public bool StopAttack(Entity bl)
    {
        var wasAttacking = bl.Attack != null;
        _attack.StopAttack(bl);
        return wasAttacking;
    }

    /// <summary>
    /// rAthena <c>unit_can_move</c> (unit.cpp:1483). True iff:
    ///   - entity isn't currently in a hit-stun freeze
    ///     (<see cref="Entity.WalkableAfterTick"/>), and
    ///   - SCs that block movement aren't active (delegated to
    ///     <see cref="EntityExtensions.CanAct"/> which reads the
    ///     OPT1 CC set + Stone/Freeze/Stun/Sleep).
    /// The full rAthena gate also checks cast state + storage opens +
    /// equip-switch lock; those are caller-side concerns today.
    /// </summary>
    public bool CanMove(Entity bl)
    {
        if (bl.WalkableAfterTick > Environment.TickCount64) return false;
        // CanAct returns false for Stone/Freeze/Stun/Sleep/etc.
        return bl.CanAct(sc: null);
    }

    /// <summary>rAthena <c>unit_attack</c> — start (or refresh) an auto-attack.</summary>
    public bool Attack(Entity src, EntityId targetId, byte continuous)
        => _attack.StartAttack(src, targetId, continuous != 0);

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

    /// <summary>
    /// rAthena <c>unit_setdir</c> (unit.cpp:1841). Writes the new
    /// facing onto <see cref="Entity.Dir"/>. The
    /// <c>ZC_CHANGE_DIRECTION</c> wire broadcast is gated on the
    /// packet definition landing in <c>Core.Server/Packets/Out/ZC/</c>
    /// — until then the server-side state is authoritative and clients
    /// learn the new facing on the next visibility refresh
    /// (<c>ZC_NOTIFY_STANDENTRY</c>). Idempotent.
    /// </summary>
    public bool SetDir(Entity bl, byte dir)
    {
        if (dir >= 8) return false;
        if (bl.Dir == dir) return true;
        bl.Dir = dir;
        return true;
    }

    /// <summary>rAthena <c>unit_getdir</c>.</summary>
    public byte GetDir(Entity bl) => bl.Dir;

    /// <summary>
    /// rAthena <c>unit_movepos</c> (unit.cpp:1898) — teleport-style
    /// move-to-cell for skills like Shadow Leap / Charge Attack. No
    /// walk frames; clears walk + attack state and re-broadcasts
    /// fixpos. Returns false if the destination cell isn't walkable
    /// (when <paramref name="checkColl"/> is true and the map is
    /// resolvable) or the entity is already there.
    /// </summary>
    public bool MovePos(Entity bl, short x, short y, byte easy, bool checkColl)
    {
        if (bl.X == x && bl.Y == y) return false;
        if (checkColl)
        {
            var map = ResolveMap(bl.MapId);
            if (map is not null && !map.IsWalkable(x, y)) return false;
        }
        _entities.Move(bl.Id, x, y);
        // rAthena: unit_movepos broadcasts clif_slide + clif_fixpos —
        // same shape as BlownBy's emission pair.
        var slide = new ZC_HIGHJUMP { SrcId = bl.Id.Value, X = x, Y = y };
        var fix = new ZC_STOPMOVE { EntityId = bl.Id.Value, X = x, Y = y };
        _visibility.SendToArea(bl, slide);
        _visibility.SendToArea(bl, fix);
        return true;
    }

    /// <summary>
    /// rAthena <c>skill_check_unit_movepos</c> (skill.cpp:9099) — try
    /// to place <paramref name="bl"/> adjacent to (<paramref name="x"/>,
    /// <paramref name="y"/>) on the same map. Scans the eight
    /// neighbouring cells in cardinal-first order; returns true once
    /// it finds a walkable one and <see cref="MovePos"/> succeeds.
    /// </summary>
    public bool CheckUnitMovePos(Entity bl, short x, short y, byte easy)
    {
        var map = ResolveMap(bl.MapId);
        // No map data? Fall back to a single MovePos attempt with collision-check skipped.
        if (map is null) return MovePos(bl, x, y, easy, checkColl: false);

        // rAthena tries the exact target cell first, then walks a 3x3
        // ring outward. Cardinal-first matches the original cell-search
        // convention; diagonal cells get checked once cardinals fail.
        // Order: (x,y) → N E S W → NE SE SW NW.
        var dx = new short[] { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
        var dy = new short[] { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
        for (var i = 0; i < dx.Length; i++)
        {
            var nx = (short)(x + dx[i]);
            var ny = (short)(y + dy[i]);
            if (!map.IsWalkable(nx, ny)) continue;
            if (nx == bl.X && ny == bl.Y) return true; // already there
            return MovePos(bl, nx, ny, easy, checkColl: false);
        }
        return false;
    }

    /// <summary>
    /// rAthena <c>unit_skilluse_id</c> — target-unit skill cast.
    /// Lazily resolves <see cref="ISkillCastService"/> through the
    /// service provider because the SkillCast → UnitOps dependency is
    /// already in the graph (skills consume UnitOps for MovePos/BlownBy).
    /// </summary>
    public bool SkillUseId(Entity src, EntityId targetId, ushort skillId, ushort skillLevel)
    {
        var castSvc = _services?.GetService(typeof(ISkillCastService)) as ISkillCastService;
        if (castSvc == null) return false;
        var result = castSvc.StartCast(src, targetId, skillId, skillLevel);
        return result == SkillCastResult.Started;
    }

    /// <summary>rAthena <c>unit_skilluse_pos</c> — ground-targeted skill cast.</summary>
    public bool SkillUsePos(Entity src, short x, short y, ushort skillId, ushort skillLevel)
    {
        var castSvc = _services?.GetService(typeof(ISkillCastService)) as ISkillCastService;
        if (castSvc == null) return false;
        var result = castSvc.StartCastAt(src, x, y, skillId, skillLevel);
        return result == SkillCastResult.Started;
    }

    /// <summary>
    /// rAthena <c>unit_remove_map</c> (unit.cpp:3107) — drop the entity
    /// from the spatial index and broadcast the vanish packet to the
    /// AOI. Caller chooses <c>clearType</c> (CLR_OUTSIGHT / CLR_DEAD /
    /// CLR_TELEPORT) which maps onto our <see cref="VanishReason"/>.
    /// Returns 1 on success, 0 if the entity was already removed.
    /// </summary>
    public int RemoveMap(Entity bl, byte clearType)
    {
        if (!_entities.Contains(bl.Id)) return 0;
        var reason = clearType switch
        {
            1 => VanishReason.Died,        // CLR_DEAD
            3 => VanishReason.Teleport,    // CLR_TELEPORT
            _ => VanishReason.Outsight,    // CLR_OUTSIGHT (default)
        };
        _visibility.NotifyVanishedToArea(bl, reason);
        _entities.Remove(bl.Id);
        return 1;
    }

    /// <summary>
    /// rAthena <c>unit_free</c> (unit.cpp:3211) — release the entity's
    /// long-lived state (walk timer, attack lock, cast). For PCs +
    /// mobs the spawn-slot release belongs with the respective spawn
    /// service; <see cref="UnitOpsService"/>'s contract is the
    /// in-flight state cleanup.
    /// </summary>
    public int Free(Entity bl, byte clearType)
    {
        _movement.CancelWalk(bl);
        _attack.StopAttack(bl);
        // Cast state isn't on Entity — ISkillCastService keeps its own
        // dictionary keyed by EntityId, so we cancel via the lazy hook.
        var castSvc = _services?.GetService(typeof(ISkillCastService)) as ISkillCastService;
        castSvc?.CancelCast(bl.Id);
        return 1;
    }

    /// <summary>
    /// rAthena <c>unit_changeviewsize</c> (unit.cpp:3289). Currently
    /// no per-entity view-size field on <see cref="Entity"/>; the
    /// canonical entry stays so consumers stop guessing. When the field
    /// lands (paired with the size_override SC family) this method
    /// writes + broadcasts <c>clif_change_view_size</c> (ZC_NOTIFY_EFFECT2).
    /// </summary>
    public int ChangeViewSize(Entity bl, short size)
    {
        // No-op until Entity.ViewSize field lands. The original rAthena
        // function is rarely-called (mob_db SizeOverride + a couple of
        // events) so the gap is documented and low-impact.
        return 0;
    }

    /// <summary>
    /// rAthena <c>unit_data_create</c> (unit.cpp:3306). The C# port
    /// allocates entity state via field initializers, so there's no
    /// heap-struct to set up — this is intentionally a no-op. Stays
    /// on the interface so atcommand / scripting call sites don't
    /// need to special-case the port architecture.
    /// </summary>
    public void DataCreate(Entity bl) { /* C# entity fields auto-init; no heap allocation. */ }

    /// <summary>
    /// rAthena <c>unit_can_reach_bl</c> (unit.cpp:1610) — is there a
    /// walkable path from <paramref name="src"/> to within
    /// <paramref name="range"/> cells of <paramref name="target"/>?
    /// </summary>
    public bool CanReachBl(Entity src, Entity target, short range)
    {
        if (target == null) return false;
        if (target.MapId != src.MapId) return false;
        // Chebyshev distance already within range → trivially reachable.
        var dist = Math.Max(Math.Abs(src.X - target.X), Math.Abs(src.Y - target.Y));
        if (dist <= range) return true;
        return CanReachPos(src, target.X, target.Y, easy: 1);
    }

    /// <summary>
    /// rAthena <c>unit_can_reach_pos</c> (unit.cpp:1574) — walkable
    /// path from <paramref name="src"/> to (<paramref name="x"/>,
    /// <paramref name="y"/>)? Uses <see cref="IPathService"/>'s
    /// pathfinder when the world registry resolves.
    /// </summary>
    public bool CanReachPos(Entity src, short x, short y, byte easy)
    {
        var map = ResolveMap(src.MapId);
        if (map == null) return true; // can't disprove in tests with no map.
        if (!map.IsWalkable(x, y)) return false;
        var path = Pathfinder.Search(map, src.X, src.Y, x, y);
        return path.Count > 0;
    }

    /// <summary>rAthena <c>unit_is_walking</c> (unit.cpp:1402).</summary>
    public bool IsWalking(Entity bl) => bl.Walk != null;

    /// <summary>
    /// rAthena <c>unit_can_attack</c> (unit.cpp:2580). Standalone
    /// predicate matching the validation set in
    /// <see cref="IAttackService.StartAttack"/> without the side
    /// effect of latching an AttackState.
    /// </summary>
    public bool CanAttack(Entity bl, Entity target)
    {
        if (target == null) return false;
        if (bl.Id == target.Id) return false;
        if (target.MapId != bl.MapId) return false;
        if (!IsTargetAlive(target)) return false;
        if (!bl.CanAct(sc: null)) return false;
        return true;

        static bool IsTargetAlive(Entity e) => e switch
        {
            PlayerEntity p => p.Hp > 0,
            MobEntity m => m.Hp > 0,
            _ => true,
        };
    }

    /// <summary>
    /// rAthena <c>unit_set_target</c> (unit.cpp:2486). Re-latches the
    /// attack target without resetting the swing timer. When no prior
    /// latch exists, delegates to <see cref="Attack"/>; when it does,
    /// only the target id moves so the existing AttackableTick is
    /// preserved and the Targeters counter transfers correctly.
    /// </summary>
    public bool SetTarget(Entity src, EntityId newTargetId)
    {
        var oldAttack = src.Attack;
        if (oldAttack == null) return Attack(src, newTargetId, continuous: 1);
        if (oldAttack.TargetId.Value == newTargetId.Value) return true; // no-op
        // Delegate through AttackService.StartAttack so Targeters
        // transfers — its same-target-same-id no-op + transfer path
        // already mirrors rAthena's unit_set_target semantics.
        return _attack.StartAttack(src, newTargetId, oldAttack.Continuous);
    }

    /// <summary>rAthena <c>unit_changetarget</c> (unit.cpp:2520) — same as <see cref="SetTarget"/>.</summary>
    public bool ChangeTarget(Entity src, EntityId newTargetId) => SetTarget(src, newTargetId);

    /// <summary>
    /// rAthena <c>unit_unattackable</c> (unit.cpp:2562). Drops the
    /// attack lock. Post-cast immunity (rAthena uses this for shield
    /// /reflect skills) is a caller concern; we only handle the lock
    /// release.
    /// </summary>
    public void Unattackable(Entity bl) => _attack.StopAttack(bl);

    /// <summary>rAthena <c>unit_skillcastcancel</c> (unit.cpp:2380).</summary>
    public bool SkillCastCancel(Entity bl)
    {
        var castSvc = _services?.GetService(typeof(ISkillCastService)) as ISkillCastService;
        return castSvc?.CancelCast(bl.Id) ?? false;
    }

    /// <summary>
    /// rAthena <c>unit_refresh</c> (unit.cpp:3148). C# parity:
    /// vanish (Outsight) then notify-spawned again so surrounding
    /// clients re-render the entity with its current wire state.
    /// Idempotent at the AOI level — clients silently discard the
    /// duplicate.
    /// </summary>
    public void Refresh(Entity bl)
    {
        if (!_entities.Contains(bl.Id)) return;
        _visibility.NotifyVanishedToArea(bl, VanishReason.Outsight);
        _visibility.NotifySpawnedToArea(bl);
    }

    /// <summary>
    /// mapId → MapData lookup. Same hashed-name convention used by
    /// <see cref="Movement.MovementService.ResolveMap"/> and
    /// <see cref="Entities.IEntityRegistry"/>; replace with a proper
    /// MapIndex when the world-side scaffold lands. Returns null if
    /// the world registry isn't injected (tests) or no map matches.
    /// </summary>
    private MapData? ResolveMap(uint mapId)
    {
        if (_world is null) return null;
        foreach (var map in _world.All)
        {
            if ((uint)map.Name.GetHashCode() == mapId) return map;
        }
        return null;
    }
}
