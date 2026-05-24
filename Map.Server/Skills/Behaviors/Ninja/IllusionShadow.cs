using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_ZANZOU — Illusion Shadow. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/illusionshadow.cpp</c>.
/// Spawns a clone mob (MOBID_ZANZOU) of the caster's name. Mob-spawn
/// IPC is TODO — animation only.
/// </summary>
public sealed class IllusionShadow : SkillImpl
{
    public IllusionShadow() : base(SkillIds.KO_ZANZOU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: full Zanzou clone spawn. rAthena mob_once_spawn_sub sets
        // master_id, special_state.ai = AI_ZANZOU, and a delete-timer keyed on
        // skill_get_time; IMobSpawnService.SpawnAt only takes (map, classId,
        // x, y) and does not expose AI flavor / master id / per-mob timers.
        // The mob_changetarget pass that redirects every nearby mob from the
        // caster to the clone has no C# equivalent either (IMobChangeTargetService
        // operates per-mob, not a foreachinrange sweep). Also: knockback the
        // target via skill_blown — UnitOps.BlownBy is available but spawning
        // without the clone makes the knockback semantically misleading.
    }
}
