using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CARTCANNON — Genetic Cart Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/cartcannon.cpp</c>.
/// Ratio: <c>+(-100 + 250*lv) + 2*INT/6</c>. Deferred: rAthena scales
/// the per-level term by <c>+20*GN_REMODELING_CART_lv</c> and shrinks
/// the INT divisor by the same amount; the Genetic Remodeling Cart
/// skill is not yet in <c>SkillIds</c>.
/// </summary>
public sealed class CartCannon : RecursiveDamageSplashSkillImpl
{
    public CartCannon() : base(SkillIds.GN_CARTCANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 250 * skillLevel) + (2 * src.Stats.IntStat / 6);
}
