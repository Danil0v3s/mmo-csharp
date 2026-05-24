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
        // Deferred: empty-bottle count check + skill_can_produce_mix for the three
        // potion ids need the produce/inventory pipeline which isn't ported yet.
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
