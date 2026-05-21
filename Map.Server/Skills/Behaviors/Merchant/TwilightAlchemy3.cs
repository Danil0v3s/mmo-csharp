using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT3 — Twilight Pharmacy III. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/twilightalchemy3.cpp</c>.
/// Brews 100 Alcohol + 50 Acid Bottle + 50 Flame Bottle in one shot;
/// requires 200 empty bottles and enough mats for each item or the
/// skill fails. Produce pipeline TODO.
/// </summary>
public sealed class TwilightAlchemy3 : SkillImpl
{
    public TwilightAlchemy3() : base(SkillIds.AM_TWILIGHT3) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: validate ITEMID_EMPTY_BOTTLE>=200 and skill_can_produce_mix for
        // ALCOHOL/ACID_BOTTLE/FIRE_BOTTLE, then mix 100/50/50.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
