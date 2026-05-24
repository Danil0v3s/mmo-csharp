using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// SS_AKUMUKESU — Nightmare Erasion. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/nightmareerasion.cpp</c>.
/// Splash dispel: ends SC_NIGHTMARE on every enemy in range.
/// </summary>
public sealed class NightmareErasion : SkillImpl
{
    public NightmareErasion() : base(SkillIds.SS_AKUMUKESU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.End(target, StatusType.Nightmare);
        // rAthena map_foreachinrange( skill_area_sub … BCT_ENEMY | SD_SPLASH | 1 )
        // dispatches the recursive nodamage call which just ends SC_NIGHTMARE
        // on each victim — replay that here. skill_db SplashArea = 7.
        if (ctx.SkillAttack != null && ctx.Sc != null)
        {
            ctx.SkillAttack.SkillAreaSub(target, 7, victim =>
            {
                if (victim.Id == target.Id) return false;
                ctx.Sc.End(victim, StatusType.Nightmare);
                return true;
            });
        }
    }
}
