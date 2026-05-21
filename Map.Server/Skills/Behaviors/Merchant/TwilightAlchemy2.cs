using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT2 — Twilight Pharmacy II. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/twilightalchemy2.cpp</c>.
/// Brews 200 Slim White Potions in one shot. Produce pipeline TODO.
/// </summary>
public sealed class TwilightAlchemy2 : SkillImpl
{
    public TwilightAlchemy2() : base(SkillIds.AM_TWILIGHT2) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: skill_produce_mix(sd, AM_TWILIGHT2, ITEMID_WHITE_SLIM_POTION, qty: 200).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
