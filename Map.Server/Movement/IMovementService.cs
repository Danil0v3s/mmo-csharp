using Map.Server.Entities;

namespace Map.Server.Movement;

/// <summary>
/// Coordinates walk requests. Validates the path, sets up <see cref="WalkState"/>,
/// and drives per-step advancement through Core.Timer scheduled callbacks.
/// </summary>
public interface IMovementService
{
    /// <summary>
    /// Request a walk from the entity's current cell to (targetX, targetY).
    /// Returns true if a valid path was found and the walk has been scheduled.
    /// Cancels any previous in-flight walk for this entity first.
    /// </summary>
    bool TryStartWalk(Entity entity, short targetX, short targetY);

    /// <summary>Stop the current walk (no-op if not walking).</summary>
    void CancelWalk(Entity entity);

    /// <summary>
    /// Wave 69 / Track B — rAthena <c>unit_set_walkdelay</c>
    /// (unit.cpp:1450). Block <paramref name="entity"/>'s movement for
    /// <paramref name="delayMs"/> milliseconds: cancels any active walk
    /// and stamps a "walkable-after" tick on the WalkState. Any
    /// `TryStartWalk` issued before the delay elapses is rejected.
    /// Idempotent — calling with a smaller delay than the current
    /// remaining freeze is a no-op.
    /// </summary>
    void SetWalkDelay(Entity entity, int delayMs);
}
