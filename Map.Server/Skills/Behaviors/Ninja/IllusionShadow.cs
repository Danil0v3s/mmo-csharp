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
        // TODO: mob_once_spawn_sub(MOBID_ZANZOU) clone + retarget mobs + knockback target.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
