using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Mob;

/// <summary>
/// Default <see cref="IMobChangeTargetService"/> impl. Pure function
/// over <see cref="MobEntity.SkillState"/> + <see cref="MobMode"/>;
/// spatial retarget consults the entity registry when wired.
/// </summary>
public sealed class MobChangeTargetService : IMobChangeTargetService
{
    private readonly IEntityRegistry? _entities;

    public MobChangeTargetService() { }
    public MobChangeTargetService(IEntityRegistry entities) { _entities = entities; }

    public bool CanChangeTarget(MobEntity mob, Entity newTarget)
    {
        if (newTarget == null) return false;

        var mode = mob.Stats.Mode;
        return mob.SkillState switch
        {
            // rAthena mob.cpp:1241 — engaged melee.
            // The full rAthena branch also fires when md->norm_attacked_id
            // matches the new target id even without MD_CHANGETARGETMELEE,
            // and has a battle_config.mob_ai&0x80 distance gate. The first
            // is irrelevant until we track per-tick attacked_id (we don't
            // yet), the second is the rAthena-default-off "aggressive
            // re-target on skill cast" branch. Conservative MVP: gate
            // strictly on MD_CHANGETARGETMELEE.
            MobSkillState.Berserk => (mode & MobMode.ChangeTargetMelee) != 0,

            // rAthena mob.cpp:1251 — chasing.
            MobSkillState.Rush => (mode & MobMode.ChangeTargetChase) != 0,

            // rAthena mob.cpp:1253-1258 — passive / idle states always
            // allow a switch.
            MobSkillState.Follow or
            MobSkillState.Angry or
            MobSkillState.Idle or
            MobSkillState.Walk or
            MobSkillState.Loot => true,

            // mob.cpp:1259-1261 default (includes MSS_DEAD / MSS_ANYTARGET).
            _ => false,
        };
    }

    public bool TrySetTarget(MobEntity mob, Entity newTarget)
    {
        if (newTarget == null) return false;

        // rAthena mob.cpp:1296 — only consult the gate when the mob
        // already has a target. The first acquisition is free.
        if (mob.TargetId != 0 && !CanChangeTarget(mob, newTarget))
            return false;

        mob.TargetId = (int)newTarget.Id.Value;
        return true;
    }

    public int RetargetMobsChasing(Entity center, short range, Entity oldTarget, Entity newTarget)
    {
        if (_entities == null || oldTarget == null || newTarget == null) return 0;
        var oldTargetId = (int)oldTarget.Id.Value;
        var switched = 0;
        // rAthena: map_foreachinallrange(unit_changetarget, src, AREA_SIZE,
        // BL_CHAR, src, target). We scope by Mob entities (only mobs
        // carry a TargetId in the C# port — PCs target via UnitData) and
        // apply the per-mob gate.
        var found = _entities.ForEachInRange(center.MapId, center.X, center.Y, range, EntityType.Mob);
        foreach (var e in found)
        {
            if (e is not MobEntity mob) continue;
            if (mob.TargetId != oldTargetId) continue;
            if (!CanChangeTarget(mob, newTarget)) continue;
            mob.TargetId = (int)newTarget.Id.Value;
            switched++;
        }
        return switched;
    }

    public Entity? TryChangeChase(MobEntity mob, short range, System.Func<Entity, bool>? canPerceive = null)
    {
        if (_entities == null) return null;
        // rAthena mob_ai_sub_hard_changechase: the first enemy already within the mob's melee reach
        // (battle_check_range(rhw.range)) that passes the enemy + visibility checks becomes the new
        // target. Enemy of a mob = a live PC; reach = Chebyshev distance ≤ range.
        foreach (var e in _entities.ForEachInRange(mob.MapId, mob.X, mob.Y, range, EntityType.Pc))
        {
            if (e is not PlayerEntity pc || pc.Hp <= 0) continue;
            if (System.Math.Max(System.Math.Abs(pc.X - mob.X), System.Math.Abs(pc.Y - mob.Y)) > range) continue;
            // AI-CHANGECHASE-VIS — rAthena status_check_skilluse: skip a candidate the mob can't perceive
            // (hidden/cloaked or LOS-blocked).
            if (canPerceive != null && !canPerceive(pc)) continue;
            return pc;
        }
        return null;
    }

    public Entity? PickRandomEnemy(MobEntity mob, short range, System.Random rng)
    {
        if (_entities == null) return null;
        // rAthena battle_getenemy: a RANDOM (not nearest) live enemy PC in range. Collect then pick.
        System.Collections.Generic.List<Entity>? pool = null;
        foreach (var e in _entities.ForEachInRange(mob.MapId, mob.X, mob.Y, range, EntityType.Pc))
        {
            if (e is not PlayerEntity pc || pc.Hp <= 0) continue;
            if (System.Math.Max(System.Math.Abs(pc.X - mob.X), System.Math.Abs(pc.Y - mob.Y)) > range) continue;
            (pool ??= new System.Collections.Generic.List<Entity>()).Add(pc);
        }
        if (pool == null || pool.Count == 0) return null;
        return pool[rng.Next(pool.Count)];
    }
}
