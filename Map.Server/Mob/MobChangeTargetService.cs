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
}
