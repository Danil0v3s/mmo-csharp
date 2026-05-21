using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_F_SIDESLIDE — Mechanic Front Side Slide. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/frontsideslide.cpp</c>.
/// Slides caster forward. Direction read TODO; broadcast only.
/// </summary>
public sealed class FrontSideSlide : SkillImpl
{
    public FrontSideSlide() : base(SkillIds.NC_F_SIDESLIDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
