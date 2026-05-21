using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_B_SIDESLIDE — Mechanic Back Side Slide. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/backsideslide.cpp</c>.
/// Slides caster backward via knockback. UnitOps.BlownBy ignores the
/// no-knockback flag here; direction lookup TODO.
/// </summary>
public sealed class BackSideSlide : SkillImpl
{
    public BackSideSlide() : base(SkillIds.NC_B_SIDESLIDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // TODO: slide via UnitOps.BlownBy with caster facing direction.
    }
}
