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
    // COMBAT-36 — optional so existing test harnesses (no ammo wiring) keep their
    // melee behavior; the real service is injected from Program.cs.
    private readonly Inventory.IAmmoService? _ammo;
    private readonly ILogger<AttackService> _logger;

    // COMBAT-70 — consulted to detect "mid-cast" so a SA_FREECAST attacker uses its
    // freecast-adjusted attack delay (Stats.FreecastAdelay) while casting.
    private readonly Map.Server.Skills.ISkillCastService? _cast;

    public AttackService(
        IEntityRegistry entities,
        IDamageService damage,
        IMovementService movement,
        ILogger<AttackService> logger,
        IStatusChangeService? sc = null,
        Inventory.IAmmoService? ammo = null,
        Map.Server.Skills.ISkillCastService? cast = null)
    {
        _entities = entities;
        _damage = damage;
        _movement = movement;
        _sc = sc;
        _ammo = ammo;
        _logger = logger;
        _cast = cast;
    }

    /// <summary>
    /// COMBAT-70 — the attack delay to schedule the next swing with. While a PC with
    /// SA_FREECAST is mid-cast, this is the precomputed <see cref="BattleStats.FreecastAdelay"/>
    /// (the `5*(lv+10)%` ASPD-base scale, status.cpp:6156); otherwise the normal
    /// <see cref="BattleStats.Adelay"/>. Non-PCs (FreecastAdelay 0) always use Adelay.
    /// </summary>
    internal int NextSwingDelay(Entity entity)
    {
        if (entity is PlayerEntity && entity.Stats.FreecastAdelay > 0
            && _cast != null && _cast.IsCasting(entity.Id))
            return entity.Stats.FreecastAdelay;
        return entity.Stats.Adelay;
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

        // Wave 71 / Track D — maintain rAthena unit_counttargeted by
        // bumping the new target's Targeters and dropping the old.
        // Same-target re-latch (Continuous refresh) is a no-op count
        // delta — only bump when the latch genuinely moves.
        var oldTargetId = source.Attack?.TargetId;
        if (oldTargetId != null && oldTargetId.Value != targetId)
        {
            var oldTarget = _entities.Get(oldTargetId.Value);
            if (oldTarget != null && oldTarget.Targeters > 0)
                oldTarget.Targeters--;
        }
        if (oldTargetId == null || oldTargetId.Value != targetId)
        {
            target.Targeters++;
        }

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

    public void StopAttack(Entity source)
    {
        // Wave 71 / Track D — release the target's Targeters slot.
        var oldTargetId = source.Attack?.TargetId;
        if (oldTargetId != null)
        {
            var oldTarget = _entities.Get(oldTargetId.Value);
            if (oldTarget != null && oldTarget.Targeters > 0)
                oldTarget.Targeters--;
        }
        source.Attack = null;
    }

    /// <summary>
    /// Wave 69 / Track B — push the entity's <see cref="AttackState.AttackableTick"/>
    /// forward. Mirrors rAthena <c>battle_damage</c>'s post-swing leg
    /// (battle.cpp:8243) where the target's attack-timer slips by its
    /// hit-stun (dmotion). No-op when the target has no AttackState or
    /// the existing AttackableTick is already further out than
    /// now+delay (longest delay wins, matches rAthena).
    /// </summary>
    public void SetAttackDelay(Entity entity, int dmotionMs)
    {
        if (dmotionMs <= 0) return;
        var state = entity.Attack;
        if (state == null) return;
        var target = Environment.TickCount64 + dmotionMs;
        if (target > state.AttackableTick)
        {
            state.AttackableTick = target;
        }
    }

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

            // COMBAT-36 — ranged ammo gate. A bow/gun with no valid equipped
            // ammo cannot swing (rAthena battle_weapon_attack → ATK_NONE,
            // battle.cpp:10386). Reschedule so we don't busy-loop, and keep the
            // AttackState (a continuous attack resumes once ammo is re-equipped,
            // matching rAthena keeping the unit_attack timer alive).
            if (entity is PlayerEntity shooter && _ammo?.HasUsableAmmo(shooter) == false)
            {
                state.AttackableTick = nowTick + Math.Max(1, NextSwingDelay(entity));
                if (!state.Continuous) StopAttack(entity);
                continue;
            }

            // Swing! battle_weapon_attack(src, target, tick, 0) in rAthena.
            // PerformMeleeAttack runs the calculator + broadcasts + applies HP.
            var result = _damage.PerformMeleeAttack(entity, target);
            // COMBAT-36 — spend one round per swing (rAthena battle_consume_ammo,
            // battle.cpp:10595). No-op for non-ammo weapons.
            if (entity is PlayerEntity ammoUser) _ammo?.ConsumeAmmo(ammoUser);
            state.HasSwung = true;
            // Schedule next swing at tick + adelay (rAthena unit.cpp:3024).
            state.AttackableTick = nowTick + Math.Max(1, NextSwingDelay(entity));

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
