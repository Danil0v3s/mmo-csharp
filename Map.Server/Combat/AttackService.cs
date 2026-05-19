using Map.Server.Entities;
using Map.Server.Movement;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Port of rAthena <c>unit_attack</c> / <c>unit_attack_timer</c>
/// (unit.cpp:2615/3056) into the C# game-loop model. Instead of one timer
/// per attacker we keep an <see cref="AttackState"/> on each entity and
/// pump them all from a single <see cref="Tick"/> call — matches the
/// rAthena semantic 1:1 because both attack-timers and game-tick run on
/// the same single thread.
/// </summary>
public sealed class AttackService : IAttackService, IAttackStopper
{
    private readonly IEntityRegistry _entities;
    private readonly IDamageService _damage;
    private readonly IMovementService _movement;
    private readonly IStatusChangeService? _sc;
    private readonly ILogger<AttackService> _logger;

    public AttackService(
        IEntityRegistry entities,
        IDamageService damage,
        IMovementService movement,
        ILogger<AttackService> logger,
        IStatusChangeService? sc = null)
    {
        _entities = entities;
        _damage = damage;
        _movement = movement;
        _sc = sc;
        _logger = logger;
    }

    public bool StartAttack(Entity source, EntityId targetId, bool continuous)
    {
        if (source.Id == targetId) return false;
        var target = _entities.Get(targetId);
        if (target == null) return false;
        if (target.MapId != source.MapId) return false;
        if (!IsAttackable(target)) return false;
        // rAthena unit_attack: pc_cant_act gates the source. STONE / FREEZE
        // / STUN / SLEEP all refuse the attack at unit.cpp:2615.
        if (!source.CanAct(_sc)) return false;

        source.Attack = new AttackState
        {
            TargetId = targetId,
            Continuous = continuous,
            // rAthena unit_attack: schedule first swing at attackabletime
            // if it's in the future, else immediately (timer = INVALID_TIMER).
            AttackableTick = source.Attack?.AttackableTick ?? 0,
            HasSwung = false,
        };
        return true;
    }

    public void StopAttack(Entity source) => source.Attack = null;

    public void Tick(long nowTick)
    {
        // Snapshot the attacker list — entities can die during the loop and
        // unregister themselves from the registry.
        foreach (var entity in _entities.All().ToArray())
        {
            var state = entity.Attack;
            if (state == null) continue;

            var target = _entities.Get(state.TargetId);
            if (target == null || target.MapId != entity.MapId || !IsAttackable(target))
            {
                StopAttack(entity);
                continue;
            }
            // rAthena unit_attack_timer guards on pc_cant_act / status_check
            // each tick. SC_FREEZE etc. acquired mid-fight stops the swing
            // train but keeps the AttackState so it resumes on SC end.
            if (!entity.CanAct(_sc))
            {
                continue;
            }

            // Range check — rAthena unit_attack_timer (unit.cpp:2962).
            // For melee (range 1) attacker must be on an adjacent cell.
            int range = Math.Max(1, (int)entity.Stats.AttackRange);
            if (!InRange(entity, target, range))
            {
                if (!state.Continuous)
                {
                    StopAttack(entity);
                    continue;
                }
                // Continuous: chase, but only if not already walking toward
                // the target's cell. We don't have unit_walktobl yet, so the
                // simplest parity move is a TryStartWalk to the target cell.
                if (entity.Walk == null)
                {
                    _movement.TryStartWalk(entity, target.X, target.Y);
                }
                continue;
            }

            if (state.AttackableTick > nowTick) continue;

            // Swing! battle_weapon_attack(src, target, tick, 0) in rAthena.
            // PerformMeleeAttack runs the calculator + broadcasts + applies HP.
            var result = _damage.PerformMeleeAttack(entity, target);
            state.HasSwung = true;
            // Schedule next swing at tick + adelay (rAthena unit.cpp:3024).
            state.AttackableTick = nowTick + Math.Max(1, (int)entity.Stats.Adelay);

            // Stop walking momentarily on swing (rAthena: unit_set_walkdelay
            // with amotion freezes movement). Implemented by killing the
            // current walk — chase will resume on the next tick if needed.
            if (entity.Walk != null)
            {
                _movement.CancelWalk(entity);
            }

            // Non-continuous attacks resolve after a single swing.
            if (!state.Continuous)
            {
                StopAttack(entity);
                continue;
            }

            // Target died on this swing? Stop here.
            if (!IsAttackable(target))
            {
                StopAttack(entity);
            }
        }
    }

    private static bool IsAttackable(Entity target) => target switch
    {
        MobEntity m => m.Hp > 0,
        PlayerEntity p => p.Hp > 0,
        _ => false,
    };

    /// <summary>Chebyshev (king-move) distance check — matches rAthena <c>check_distance_bl</c>.</summary>
    private static bool InRange(Entity a, Entity b, int range)
    {
        var dx = Math.Abs(a.X - b.X);
        var dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy) <= range;
    }
}
