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
}
