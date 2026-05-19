using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Narrow seam used by warp / death paths to halt an in-flight
/// auto-attack without taking a hard dependency on the full
/// <see cref="IAttackService"/>. The wider service is implemented by
/// <c>AttackService</c>; this interface only exists to break DI
/// cycles (attack → damage → setpos/death → attack).
/// </summary>
public interface IAttackStopper
{
    /// <summary>Mirrors <see cref="IAttackService.StopAttack"/>.</summary>
    void StopAttack(Entity source);
}
