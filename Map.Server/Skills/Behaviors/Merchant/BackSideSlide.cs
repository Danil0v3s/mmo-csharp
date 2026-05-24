using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_B_SIDESLIDE — Mechanic Back Side Slide. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/backsideslide.cpp</c>.
/// rAthena skill.cpp:11822: blewcount = 7 in caster's facing direction.
/// </summary>
public sealed class BackSideSlide : SkillImpl
{
    public BackSideSlide() : base(SkillIds.NC_B_SIDESLIDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.UnitOps != null)
        {
            var dir = ctx.UnitOps.GetDir(src);
            ctx.UnitOps.BlownBy(src, dir, count: 7);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
