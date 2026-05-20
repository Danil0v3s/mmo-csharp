using Map.Server.Entities;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleTargetService"/>.
///
/// <para><see cref="GetTarget"/> reads from <c>Entity.Attack</c>;
/// <see cref="GetTargeted"/> sweeps the registry for any entity
/// whose AttackState points at <paramref name="target"/>.
/// <see cref="GetEnemy"/> picks the closest entity inside the
/// Chebyshev range whose <see cref="DamageService.CanDamage"/>
/// check passes.</para>
///
/// <para>SC-driven shields (<see cref="IsInfiniteDefense"/>) read
/// the SC list directly so this service stays leaf-level. Coma is
/// a roll today; the full bonus-script port will surface the
/// per-target rate when it lands.</para>
/// </summary>
public sealed class BattleTargetService : IBattleTargetService
{
    private readonly IEntityRegistry _entities;
    private readonly IStatusChangeService? _sc;
    private readonly ILogger<BattleTargetService> _logger;
    private static readonly Random Rng = new();

    public BattleTargetService(
        IEntityRegistry entities,
        ILogger<BattleTargetService> logger,
        IStatusChangeService? sc = null)
    {
        _entities = entities;
        _logger = logger;
        _sc = sc;
    }

    public int GetTarget(Entity bl) => bl.Attack?.TargetId.Value ?? 0;

    public IEnumerable<Entity> GetTargeted(Entity target)
    {
        foreach (var e in _entities.All())
        {
            if (e.Id == target.Id) continue;
            if (e.Attack?.TargetId.Value == target.Id.Value) yield return e;
        }
    }

    public Entity? GetEnemy(Entity bl, int type, int range)
    {
        Entity? best = null;
        var bestDist = int.MaxValue;
        foreach (var e in _entities.All())
        {
            if (e.Id == bl.Id) continue;
            if (e.MapId != bl.MapId) continue;
            // Type filter: match the rAthena BL_PC / BL_MOB bitmask
            // shape. We use EntityType directly; type==0 means "any".
            if (type != 0 && (int)e.Type != type) continue;
            var dist = Math.Max(Math.Abs(e.X - bl.X), Math.Abs(e.Y - bl.Y));
            if (dist > range) continue;
            if (dist < bestDist)
            {
                best = e;
                bestDist = dist;
            }
        }
        return best;
    }

    public Entity? GetMaster(Entity bl)
        => bl.MasterId is { } id ? _entities.Get(id) : null;

    public ushort GetCurrentSkill(Entity bl)
    {
        // Read the in-flight skill from the attack state's "last
        // cast" field when it ports. Until then we return 0; the
        // caller can fall back to its own tracking.
        return 0;
    }

    public bool CheckUndead(BattleRace race, BattleElement defenseElement)
        => race == BattleRace.Undead || defenseElement == BattleElement.Undead;

    public bool CheckComa(Entity src, Entity target)
    {
        // rAthena coma proc reads sd->bonus.coma_class / coma_race
        // arrays — those don't exist on BattleStats yet. Canonical
        // entry exists; aggregator wiring lands when equip-bonus
        // accumulators do.
        return false;
    }

    public bool IsInfiniteDefense(Entity target, BattleAttackType type)
    {
        // Steel Body + variants set SC_STEELBODY which gives 90% reduction
        // (effectively infinite vs auto-attacks). The full set:
        //   SC_STEELBODY     (Monk Steel Body)
        //   SC_GVG_GIANT     (GvG event Giant body)
        //   SC_INVINCIBLE    (GM /invincible)
        //   SC_NO_RECOVER_STATE
        // None of these are registered SC types yet. Return false; the
        // canonical entry stays here so the resolver can call once
        // the SCs port.
        return false;
    }
}
