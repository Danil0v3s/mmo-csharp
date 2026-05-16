using Core.Timer;
using Map.Server.Entities;
using Map.Server.Visibility;
using Map.Server.World;

namespace Map.Server.Movement;

/// <summary>
/// MS1 walk implementation. Each step is scheduled via <see cref="Scheduler"/>
/// (the Core.Timer event-driven scheduler), matching rAthena's
/// <c>unit_walktoxy_timer</c> approach. Step callbacks fire on the scheduler's
/// thread pool; mutations to entity position go through
/// <see cref="IEntityRegistry.Move"/> which is thread-safe.
///
/// After each step the per-cell view-diff in <see cref="IVisibilityService"/>
/// fires the rAthena <c>clif_outsight</c> / <c>clif_insight</c> pair:
/// STANDENTRY for entities the walker just brought into mutual view, VANISH
/// for those that just dropped out.
/// </summary>
public sealed class MovementService : IMovementService
{
    private readonly IEntityRegistry _registry;
    private readonly IMapWorldRegistry _worldRegistry;
    private readonly IVisibilityService _visibility;
    private readonly ILogger<MovementService> _logger;

    public MovementService(
        IEntityRegistry registry,
        IMapWorldRegistry worldRegistry,
        IVisibilityService visibility,
        ILogger<MovementService> logger)
    {
        _registry = registry;
        _worldRegistry = worldRegistry;
        _visibility = visibility;
        _logger = logger;
    }

    public bool TryStartWalk(Entity entity, short targetX, short targetY)
    {
        var map = ResolveMap(entity.MapId);
        if (map == null) return false;

        // Cancel any prior walk before recomputing.
        if (entity.Walk != null)
        {
            CancelWalk(entity);
        }

        var path = Pathfinder.Search(map, entity.X, entity.Y, targetX, targetY);
        if (path.Count == 0) return false;

        entity.Walk = new WalkState(path, targetX, targetY);
        ScheduleNextStep(entity);
        return true;
    }

    public void CancelWalk(Entity entity)
    {
        var walk = entity.Walk;
        if (walk == null) return;
        if (!walk.NextStepTimer.Equals(Scheduler.InvalidTimer))
        {
            Scheduler.Cancel(walk.NextStepTimer);
        }
        entity.Walk = null;
    }

    private MapData? ResolveMap(uint mapId)
    {
        // Same convention as EntityRegistry: mapId is the hashed map name.
        // Replace with proper MapIndex when world.md's Pending lands.
        foreach (var map in _worldRegistry.All)
        {
            if ((uint)map.Name.GetHashCode() == mapId) return map;
        }
        return null;
    }

    private void ScheduleNextStep(Entity entity)
    {
        var walk = entity.Walk;
        if (walk == null || walk.IsFinished)
        {
            entity.Walk = null;
            return;
        }

        var dir = walk.RemainingPath.Peek();
        var delayMs = dir.IsDiagonal()
            ? entity.Speed * Pathfinder.MoveDiagonalCost / Pathfinder.MoveCost
            : entity.Speed;

        walk.NextStepTimer = Scheduler.Schedule(
            asyncCallback: OnStepCallback,
            state: entity.Id,
            dueTime: TimeSpan.FromMilliseconds(delayMs));
    }

    private ValueTask OnStepCallback(object? state, TimerId timer, long scheduledTick)
    {
        if (state is not EntityId id) return ValueTask.CompletedTask;
        var entity = _registry.Get(id);
        if (entity?.Walk == null) return ValueTask.CompletedTask;

        var walk = entity.Walk;
        // Re-validate: the entity might have been cancelled+restarted on
        // another path between scheduling and firing. Compare the timer id.
        if (!walk.NextStepTimer.Equals(timer)) return ValueTask.CompletedTask;

        if (walk.RemainingPath.Count == 0)
        {
            entity.Walk = null;
            return ValueTask.CompletedTask;
        }

        var dir = walk.RemainingPath.Dequeue();
        var nextX = (short)(entity.X + dir.Dx());
        var nextY = (short)(entity.Y + dir.Dy());

        // Re-check walkability: dynamic obstacles (Ice Wall, etc.) may have
        // appeared since we pathfound. For MS1 cells are static — this is
        // future-proofing.
        var map = ResolveMap(entity.MapId);
        if (map == null || !map.IsWalkable(nextX, nextY))
        {
            _logger.LogDebug("Walk interrupted for entity {Id}: next cell not walkable", id);
            entity.Walk = null;
            return ValueTask.CompletedTask;
        }

        var fromX = entity.X;
        var fromY = entity.Y;
        try
        {
            _registry.Move(id, nextX, nextY);
            entity.Dir = (byte)dir;
        }
        catch (InvalidOperationException)
        {
            // Entity was removed during the walk (disconnect).
            entity.Walk = null;
            return ValueTask.CompletedTask;
        }

        // AOI edge update — anyone walking into / out of view of the entity
        // (or vice versa) gets exactly one STANDENTRY or VANISH per step.
        _visibility.NotifyMoveDiff(entity, fromX, fromY, nextX, nextY);

        if (walk.RemainingPath.Count == 0)
        {
            entity.Walk = null;
            return ValueTask.CompletedTask;
        }

        ScheduleNextStep(entity);
        return ValueTask.CompletedTask;
    }
}
