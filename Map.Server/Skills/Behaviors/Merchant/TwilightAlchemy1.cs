using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// AM_TWILIGHT1 — Twilight Pharmacy I (skill.cpp:AM_TWILIGHT1 arm).
/// Bulk-brews 200 White Potions. Routes through the AM_PHARMACY
/// recipe row that produces item 504 (White Potion); ProduceMix
/// consumes the per-bottle materials × 200 and adds the stack.
/// </summary>
public sealed class TwilightAlchemy1 : SkillImpl
{
    /// <summary>rAthena ITEMID_WHITE_POTION.</summary>
    private const uint WhitePotionId = 504;
    /// <summary>rAthena twilight bulk batch size.</summary>
    private const int BatchQty = 200;

    public TwilightAlchemy1() : base(SkillIds.AM_TWILIGHT1) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity pc) return;
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        var recipeId = FindPharmacyRecipe(ctx, WhitePotionId);
        if (recipeId < 0) return;
        ctx.Production?.ProduceMix(pc, recipeId, BatchQty);
    }

    internal static int FindPharmacyRecipe(SkillBehaviorContext ctx, uint produceItemId)
    {
        if (ctx.Recipes == null) return -1;
        var pharmacyRecipes = ctx.Recipes.GetByRequireSkill(SkillIds.AM_PHARMACY, byte.MaxValue);
        foreach (var r in pharmacyRecipes)
            if (r.ProduceItemId == produceItemId) return r.Id;
        return -1;
    }
}
