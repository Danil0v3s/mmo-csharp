using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT2 — Twilight Pharmacy II (skill.cpp:AM_TWILIGHT2 arm).
/// Bulk-brews 200 Slim White Potions (item 547) via the matching
/// AM_PHARMACY recipe.
/// </summary>
public sealed class TwilightAlchemy2 : SkillImpl
{
    /// <summary>rAthena ITEMID_SLIM_WHITE_POTION.</summary>
    private const uint SlimWhitePotionId = 547;
    private const int BatchQty = 200;

    public TwilightAlchemy2() : base(SkillIds.AM_TWILIGHT2) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var recipeId = TwilightAlchemy1.FindPharmacyRecipe(ctx, SlimWhitePotionId);
        if (recipeId < 0) return;
        ctx.Production?.ProduceMix(pc, recipeId, BatchQty);
    }
}
