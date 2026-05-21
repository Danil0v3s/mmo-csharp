using Map.Server.Entities;

namespace Map.Server.Mob;

/// <summary>
/// Resolves <see cref="MobSkillTarget"/> values to a concrete
/// <see cref="Entity"/> or (x, y) cell at cast time. Mirrors the two
/// target-resolution switches in rAthena <c>mobskill_use</c>
/// (<c>mob.cpp:4392-4475</c> for ground skills, <c>mob.cpp:4445-4475</c>
/// for single-target skills).
///
/// <para>The resolver is a deliberate read-only helper — it never
/// mutates the mob; the caller picks up the result and decides whether
/// to call <c>unit_skilluse_id2</c> or <c>unit_skilluse_pos2</c>.</para>
/// </summary>
public sealed class MobSkillTargetResolver
{
    private readonly IEntityRegistry _entities;
    private readonly Random _rng;

    public MobSkillTargetResolver(IEntityRegistry entities, Random? rng = null)
    {
        _entities = entities;
        _rng = rng ?? Random.Shared;
    }

    /// <summary>
    /// Resolve a target-entity for a non-ground skill. Mirrors
    /// <c>mob.cpp:4445-4475</c>. Returns null if no valid target
    /// could be found (caller should continue / break per
    /// <c>battle_config.mob_ai &amp; 0x1000</c>).
    /// </summary>
    public Entity? ResolveEntity(MobEntity mob, MobSkillTarget mode)
    {
        switch (mode)
        {
            case MobSkillTarget.Target:
                // The mob's current combat target. If absent and the mob
                // can't attack, fall back to attacked_id (rAthena says:
                // "Monsters that cannot attack put their last attacker
                // as target").
                if (mob.TargetId != 0)
                {
                    var t = _entities.Get(new EntityId(mob.TargetId));
                    if (t != null) return t;
                }
                if ((mob.Stats.Mode & Map.Server.Status.MobMode.CanAttack) == 0 && mob.AttackedId != 0)
                {
                    return _entities.Get(new EntityId(mob.AttackedId));
                }
                return null;

            case MobSkillTarget.Self:
                return mob;

            case MobSkillTarget.Master:
                // Master entity (slave-mob's owner). Fallback: self,
                // matching rAthena's `bl = md; if (master_id) bl = ...`
                if (mob.MasterId is { } masterId)
                {
                    var master = _entities.Get(masterId);
                    if (master != null) return master;
                }
                return mob;

            case MobSkillTarget.Friend:
                // The "friend" entity tracked alongside the condition
                // evaluation (mob_getfriendhprate / mob_getfriendstatus
                // populate `fbl`/`fmd`). Until the friend-tracker lands
                // we fall back to self so the cast still resolves on
                // a valid block_list — matches rAthena's last-resort
                // `bl = md`.
                return mob;

            case MobSkillTarget.Random:
                // Pick a random hostile entity within skill_range2.
                // Range defaults to AOI view (rAthena uses
                // skill_get_range2 which we don't have access to here);
                // 9 is the conservative inner ring most damaging mob
                // skills target.
                return ResolveRandomEnemy(mob, range: 9);

            // Around1-8 / Around5-8 are ground modes; for entity
            // resolution they degrade to Target (rAthena says the
            // bl is the target, then the cell offset is computed
            // separately for ground casts).
            case MobSkillTarget.Around1:
            case MobSkillTarget.Around2:
            case MobSkillTarget.Around3:
            case MobSkillTarget.Around4:
            case MobSkillTarget.Around5:
            case MobSkillTarget.Around6:
            case MobSkillTarget.Around7:
            case MobSkillTarget.Around8:
                if (mob.TargetId != 0)
                {
                    var t = _entities.Get(new EntityId(mob.TargetId));
                    if (t != null) return t;
                }
                return mob;

            default:
                return mob;
        }
    }

    /// <summary>
    /// Resolve a (x, y) cast cell for a ground skill. Mirrors
    /// <c>mob.cpp:4421-4433</c>:
    /// <list type="bullet">
    ///   <item>For <see cref="MobSkillTarget.Around1"/> through <see cref="MobSkillTarget.Around8"/>,
    ///         pick a random cell within N tiles of the base entity (N derived
    ///         from <c>(target - MST_AROUND1) + 1</c> or
    ///         <c>(target - MST_AROUND5) + 1</c>).</item>
    ///   <item>Otherwise the cast cell is the base entity's
    ///         <see cref="Entity.X"/> / <see cref="Entity.Y"/>.</item>
    /// </list>
    /// Returns null if the base entity could not be resolved.
    /// </summary>
    public (short x, short y)? ResolveGroundCell(MobEntity mob, MobSkillTarget mode)
    {
        var bl = ResolveEntity(mob, mode);
        if (bl == null) return null;

        var x = bl.X;
        var y = bl.Y;

        // rAthena MST_AROUND1..MST_AROUND4 (values 9..12) → range 1..4
        //         MST_AROUND5..MST_AROUND8 (values 5..8) → range 1..4
        int range = mode switch
        {
            MobSkillTarget.Around1 => 1,
            MobSkillTarget.Around2 => 2,
            MobSkillTarget.Around3 => 3,
            MobSkillTarget.Around4 => 4,
            MobSkillTarget.Around5 => 1,
            MobSkillTarget.Around6 => 2,
            MobSkillTarget.Around7 => 3,
            MobSkillTarget.Around8 => 4,
            _ => 0,
        };
        if (range > 0)
        {
            x = (short)(bl.X + _rng.Next(-range, range + 1));
            y = (short)(bl.Y + _rng.Next(-range, range + 1));
        }
        return (x, y);
    }

    // --- helpers ---

    private Entity? ResolveRandomEnemy(MobEntity mob, short range)
    {
        // rAthena: battle_getenemy iterates BL_CHAR in range, filters by
        // DEFAULT_ENEMY_TYPE, returns a uniform random pick. Until the
        // BattleTargetService gains a GetRandomEnemy, we use the
        // EntityRegistry range scan + manual filter.
        var candidates = _entities.ForEachInRange(
            mob.MapId, mob.X, mob.Y, range, EntityType.Pc | EntityType.Mob);
        var living = candidates
            .Where(e => e.Id != mob.Id && IsAlive(e) && IsHostile(mob, e))
            .ToList();
        if (living.Count == 0) return null;
        return living[_rng.Next(living.Count)];
    }

    private static bool IsAlive(Entity e) => e switch
    {
        PlayerEntity p => p.Hp > 0,
        MobEntity m => m.Hp > 0,
        _ => true,
    };

    private static bool IsHostile(MobEntity src, Entity tgt)
    {
        // Players are always hostile; mobs are hostile if they aren't
        // our slaves (master_id-based). Matches the AI-NONE check in
        // mob_target.
        if (tgt is PlayerEntity) return true;
        if (tgt is MobEntity m)
        {
            // Same-master slaves of a player aren't hostile to each other.
            return m.MasterId != src.Id && (m.MasterId == null || m.MasterId != src.MasterId);
        }
        return false;
    }
}
