using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT3 — Twilight Pharmacy III (skill.cpp:AM_TWILIGHT3 arm).
/// Bulk-brews 100 Alcohol + 50 Acid Bottle + 50 Flame Bottle via
/// three AM_PHARMACY recipes. rAthena requires 200 empty bottles
/// (Materials column on the matching recipes); ProduceMix runs each
/// recipe independently, refusing if any material is short.
/// </summary>
public sealed class TwilightAlchemy3 : SkillImpl
{
    /// <summary>rAthena ITEMID_ALCOHOL.</summary>
    private const uint AlcoholId = 970;
    /// <summary>rAthena ITEMID_ACID_BOTTLE.</summary>
    private const uint AcidBottleId = 7135;
    /// <summary>rAthena ITEMID_FIRE_BOTTLE.</summary>
    private const uint FireBottleId = 7136;

    public TwilightAlchemy3() : base(SkillIds.AM_TWILIGHT3) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        if (ctx.Production == null) return;
        var alcohol = TwilightAlchemy1.FindPharmacyRecipe(ctx, AlcoholId);
        var acid = TwilightAlchemy1.FindPharmacyRecipe(ctx, AcidBottleId);
        var fire = TwilightAlchemy1.FindPharmacyRecipe(ctx, FireBottleId);
        if (alcohol >= 0) ctx.Production.ProduceMix(pc, alcohol, 100);
        if (acid >= 0) ctx.Production.ProduceMix(pc, acid, 50);
        if (fire >= 0) ctx.Production.ProduceMix(pc, fire, 50);
    }
}
