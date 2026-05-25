using Map.Server.Entities;

namespace Map.Server.Combat;

/// <summary>
/// Drives the continuous attack timer. rAthena's <c>unit_attack</c>
/// (unit.cpp:2615) plus the <c>unit_attack_timer</c> tick loop
/// (unit.cpp:3056) collapsed into one service.
///
/// One call to <see cref="StartAttack"/> latches the target on the
/// attacker; <see cref="Tick"/> runs every game tick from
/// <c>MapServerImpl.UpdateGameLogicAsync</c> and fires each swing when
/// <c>AttackableTick</c> has elapsed. Range / death / map / continuous-vs-
/// one-shot break conditions are checked every tick — matches rAthena
/// behavior so a fleeing target naturally drops the attack lock.
/// </summary>
public interface IAttackService
{
    /// <summary>
    /// Start (or refresh) an attack against <paramref name="targetId"/>.
    /// <paramref name="continuous"/> = true: autoattack; false: single swing.
    /// Returns true if the attack started, false on validation failure
    /// (target missing / dead / cross-map / not enemy).
    /// </summary>
    bool StartAttack(Entity source, EntityId targetId, bool continuous);

    /// <summary>Cancel any in-flight attack on this entity. Idempotent.</summary>
    void StopAttack(Entity source);

    /// <summary>
    /// Wave 69 / Track B — push <paramref name="entity"/>'s
    /// <see cref="AttackState.AttackableTick"/> forward by
    /// <paramref name="dmotionMs"/>. Mirrors the post-swing leg of
    /// rAthena <c>battle_damage</c> (battle.cpp:8243) — a freshly hit
    /// target can't swing again until its hit-stun animation finishes.
    /// No-op when the target has no AttackState (idle entity) or the
    /// existing AttackableTick is already further out than now+delay.
    /// </summary>
    void SetAttackDelay(Entity entity, int dmotionMs);

    /// <summary>
    /// Process one tick of attack timers across all attacking entities.
    /// Called from the game loop (60 FPS for map, mob AI also drives this).
    /// </summary>
    void Tick(long nowTick);
}
