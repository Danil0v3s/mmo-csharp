using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// AB_SILENTIUM — Arch Bishop Silentium. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/silentium.cpp</c>.
///
/// <para>AoE silence centered on the caster. rAthena iterates every
/// enemy in <c>skill_get_splash</c> and applies SC_SILENCE on each
/// (skill_castend_nodamage_id with PR_LEXDIVINA as the recursion
/// target — same SC duration formula). The cast frame is emitted
/// once on the primary target.</para>
/// </summary>
public sealed class Silentium : SkillImpl
{
    public Silentium() : base(SkillIds.AB_SILENTIUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena: map_foreachinallrange(skill_area_sub, src, splash, BL_CHAR,
        //   src, PR_LEXDIVINA, skill_lv, tick, flag|BCT_ENEMY|1, skill_castend_nodamage_id);
        const short splashRange = 7; // AB_SILENTIUM skill_db splash
        var duration = (25 + 5 * skillLevel) * 1000; // matches PR_LEXDIVINA

        var victims = ctx.Entities.ForEachInRange(src.MapId, src.X, src.Y, splashRange,
            EntityType.Mob | EntityType.Pc)
            .Where(v => v.Id != src.Id);

        foreach (var v in victims)
        {
            ctx.Sc?.Start(v, StatusType.Silence, val1: skillLevel,
                0, 0, 0, duration, src);
        }

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
