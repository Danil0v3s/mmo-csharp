using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT1 — Twilight Pharmacy I. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/twilightalchemy1.cpp</c>.
/// Brews 200 White Potions in one shot. The produce pipeline isn't
/// wired yet, so we broadcast the animation and TODO the mix call.
/// </summary>
public sealed class TwilightAlchemy1 : SkillImpl
{
    public TwilightAlchemy1() : base(SkillIds.AM_TWILIGHT1) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity) return;
        // TODO: skill_produce_mix(sd, AM_TWILIGHT1, ITEMID_WHITE_POTION, qty: 200).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
