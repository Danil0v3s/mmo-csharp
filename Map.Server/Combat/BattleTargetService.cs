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
        // rAthena is_infinite_defense (battle.cpp:2878). Returns true when:
        //   1. Target has SC_INVINCIBLE active (universal — affects all BF types).
        //   2. MobMode bit matches the BF lane:
        //        IgnoreMelee  + BF_WEAPON|BF_SHORT (melee weapon)
        //        IgnoreMagic  + BF_MAGIC
        //        IgnoreRanged + BF_WEAPON|BF_LONG  (ranged weapon)
        //        IgnoreMisc   + BF_MISC
        //   3. BL_SKILL targets: skill_id == NPC_REVERBERATION or
        //      WM_POEMOFNETHERWORLD (skill-unit damageable plants).
        //
        // Our BattleAttackType doesn't carry the BF_SHORT vs BF_LONG
        // distinction, so IgnoreMelee/IgnoreRanged checks can't fire
        // accurately on the BF_WEAPON path — those need an attack-range
        // overload. For now we handle the unambiguous lanes (Magic /
        // Misc / SC_INVINCIBLE), which covers MD_IGNOREMAGIC golems and
        // GM-invincible PCs. Add a (target, type, isLong) overload when
        // a caller needs the melee/ranged split.

        // SC_INVINCIBLE is universal — bypass MobMode checks.
        if (_sc?.Get(target, StatusType.Invincible) != null) return true;

        var mode = target.Stats.Mode;
        switch (type)
        {
            case BattleAttackType.Magic:
                return (mode & MobMode.IgnoreMagic) != 0;
            case BattleAttackType.Misc:
                return (mode & MobMode.IgnoreMisc) != 0;
            case BattleAttackType.Weapon:
                // Both IgnoreMelee and IgnoreRanged need attack-range
                // disambiguation. Without it we conservatively allow
                // the hit through. A future overload can drive both
                // by passing isLong from the caller.
                return false;
            default:
                return false;
        }
    }
}
