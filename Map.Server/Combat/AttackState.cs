using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Per-entity attack-loop state. Mirrors the subset of rAthena
/// <c>unit_data</c> the autoattack tick reads: target, swing-cadence
/// gating, continuous flag, chase range. Owned by
/// <see cref="IAttackService"/> — gameplay code reads, the service writes.
/// </summary>
public sealed class AttackState
{
    public EntityId TargetId;
    /// <summary>
    /// Tick (Environment.TickCount64) at which the next swing is allowed.
    /// Mirrors <c>ud.attackabletime</c>.
    /// </summary>
    public long AttackableTick;

    /// <summary>True for autoattack (DMG_REPEAT = 7); false for one-shot (DMG_NORMAL = 0).</summary>
    public bool Continuous;

    /// <summary>True after the first swing fires — used so single-shots terminate after 1 hit.</summary>
    public bool HasSwung;
}
