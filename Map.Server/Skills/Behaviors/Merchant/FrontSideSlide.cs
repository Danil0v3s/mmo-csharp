using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// NC_F_SIDESLIDE — Mechanic Front Side Slide. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/frontsideslide.cpp</c>.
/// Knocks the caster 7 cells along <c>(GetDir(src) + 4) % 8</c> — i.e.
/// the cell opposite the caster's facing, so they slide "forward" on
/// the world axis (rAthena <c>skill_blown</c> with
/// <c>BLOWN_IGNORE_NO_KNOCKBACK</c>). Sibling of <see cref="BackSideSlide"/>
/// which uses the raw facing direction.
/// </summary>
public sealed class FrontSideSlide : SkillImpl
{
    public FrontSideSlide() : base(SkillIds.NC_F_SIDESLIDE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.UnitOps != null)
        {
            var dir = (byte)((ctx.UnitOps.GetDir(src) + 4) % 8);
            ctx.UnitOps.BlownBy(src, dir, count: 7);
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
