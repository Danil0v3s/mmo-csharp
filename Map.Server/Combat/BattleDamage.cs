namespace Map.Server.Combat;

/// <summary>
/// Outcome of a single damage calc. Mirrors rAthena <c>struct Damage</c>
/// (battle.hpp), trimmed to what the C# combat path uses.
///
/// <see cref="Type"/> drives the <see cref="Core.Server.Packets.Out.ZC.DamageActionType"/>
/// on <c>ZC_NOTIFY_ACT3</c>; non-Normal codes (Flee/Critical/Lucky)
/// change the client-side animation even when <see cref="Total"/> is 0.
/// </summary>
public sealed class BattleDamage
{
    /// <summary>
    /// Right-hand weapon damage after all reductions. 0 when the attack
    /// missed (Type = Flee). Multi-hit attacks divide <see cref="Total"/>
    /// across <see cref="Hits"/>.
    /// </summary>
    public long Damage;

    /// <summary>Left-hand weapon damage (dual wield). 0 when not dual-wielding.</summary>
    public long Damage2;

    /// <summary>Number of hits — Bash/Double Attack/etc. fire multiple swings in one action.</summary>
    public int Hits = 1;

    /// <summary>
    /// Composed for the wire packet:
    /// Normal/Critical/Flee/Lucky (perfect dodge)/Multi-hit/etc.
    /// </summary>
    public Core.Server.Packets.Out.ZC.DamageActionType Type =
        Core.Server.Packets.Out.ZC.DamageActionType.Normal;

    /// <summary>True if the attack landed (hit roll passed). False
    /// for Flee misses and Lucky-dodge (perfect-dodge) outcomes.</summary>
    public bool DidHit =>
        Type != Core.Server.Packets.Out.ZC.DamageActionType.Flee &&
        Type != Core.Server.Packets.Out.ZC.DamageActionType.LuckyDodge;

    /// <summary>True if rolled a critical (always-hits + 1.4× damage in renewal).</summary>
    public bool IsCritical => Type == Core.Server.Packets.Out.ZC.DamageActionType.Critical;

    /// <summary>Sum of left + right damage. Zero on miss.</summary>
    public long Total => Damage + Damage2;

    /// <summary>
    /// Wave 66 / Track B — rAthena <c>Damage.dmotion</c> (battle.hpp).
    /// Hit-stun (ms) on the target: their next auto-attack timer is
    /// pushed back by this duration. Derived from the target's Amotion
    /// in <see cref="BattleCalculator"/>; consumed by
    /// <see cref="DamageService"/> via the target's
    /// <see cref="AttackState.AttackableTick"/>.
    /// </summary>
    public int DMotion;

    /// <summary>
    /// Wave 66 / Track B — rAthena <c>Damage.walkdelay</c>. Movement
    /// freeze (ms) on the target: their <c>unit_set_walkdelay</c> kicks
    /// for this duration. Zero on miss. Consumed by
    /// <see cref="DamageService"/> via <c>IMovementService</c>.
    /// </summary>
    public int WalkDelay;

    /// <summary>
    /// Wave 66 / Track C — rAthena <c>Damage.isspdamage</c>. True when
    /// the damage delta lands on SP, not HP (Soul Drain, Soul Breaker,
    /// SP-vampire skills). Drives <c>ZC_NOTIFY_ACT3.IsSpDamage</c> on
    /// the wire so the client renders the orange numbers.
    /// </summary>
    public bool IsSpDamage;
}
