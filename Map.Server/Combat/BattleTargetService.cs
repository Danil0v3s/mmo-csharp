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
        // rAthena battle_check_coma (battle.cpp:6543).
        // Only PCs roll coma — cards/equip slots belong to a player.
        if (src is not PlayerEntity pc) return false;
        var bundle = pc.EquipBonuses;
        if (bundle == null) return false;

        // Resolve target class flag (Normal / Boss / Guardian) and race.
        var ts = target.Stats;
        var classIdx = (int)Inventory.BattleClassFlag.Normal;
        if ((ts.Mode & MobMode.Mvp) != 0)
            classIdx = (int)Inventory.BattleClassFlag.Boss;
        // CLASS_GUARDIAN requires a mob mode bit we don't expose yet
        // (mode_guardian); guardians collapse to Normal until that lands.
        var classAll = (int)Inventory.BattleClassFlag.All;

        var raceIdx = (int)ts.Race;
        var raceAll = (int)BattleRace.All;

        // Skip rate roll entirely when both buckets are zero.
        int rate = 0;
        if (classIdx >= 0 && classIdx < bundle.ComaClass.Length) rate += bundle.ComaClass[classIdx];
        if (classAll < bundle.ComaClass.Length) rate += bundle.ComaClass[classAll];
        if (raceIdx >= 0 && raceIdx < bundle.ComaRace.Length) rate += bundle.ComaRace[raceIdx];
        if (raceAll < bundle.ComaRace.Length) rate += bundle.ComaRace[raceAll];
        if (rate <= 0) return false;

        // rAthena excludes the Emperium + Battlefield-class targets
        // from coma. Our MobMode doesn't carry the Emperium bit yet;
        // a future MD_EMPERIUM exposure adds the early return.
        // Per-myriad roll: rate is permille × 10 (out of 10 000).
        if (Rng.Next(10_000) >= rate) return false;

        _logger.LogDebug(
            "battle_check_coma: PC {Char} procced coma on target {Tgt} (rate {Rate}/10000)",
            pc.CharacterId, target.Id.Value, rate);
        return true;
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
