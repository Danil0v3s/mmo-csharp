using Core.Timer;

namespace Map.Server.Movement;

/// <summary>
/// Per-entity walk state. Lives on <c>PlayerEntity.Walk</c> and (in MS2) on
/// <c>MobEntity.Walk</c>. Null when the entity is standing.
///
/// The entity steps along <see cref="RemainingPath"/> one cell at a time
/// driven by Core.Timer scheduled callbacks. Each successful step pops the
/// front direction and schedules the next.
/// </summary>
public sealed class WalkState
{
    /// <summary>Path remaining from the entity's current cell (FIFO of directions).</summary>
    public Queue<Direction> RemainingPath { get; }

    /// <summary>Final target cell — used for early-abort or arrival check.</summary>
    public short TargetX { get; }
    public short TargetY { get; }

    /// <summary>Active scheduled-step timer (so we can cancel on interrupt).</summary>
    public TimerId NextStepTimer { get; set; }

    public WalkState(IEnumerable<Direction> path, short targetX, short targetY)
    {
        RemainingPath = new Queue<Direction>(path);
        TargetX = targetX;
        TargetY = targetY;
        NextStepTimer = Scheduler.InvalidTimer;
    }

    public bool IsFinished => RemainingPath.Count == 0;
}
