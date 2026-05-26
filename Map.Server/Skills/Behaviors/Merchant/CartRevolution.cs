using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_CARTREVOLUTION — Merchant Cart Revolution. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/cartrevolution.cpp</c>.
/// Ratio: <c>+50</c> base + cart-weight scale (rAthena adds
/// <c>+100 * cart_weight / cart_weight_max</c>; non-player casters
/// add the flat <c>+100</c> max).
///
/// <para>Cart weight + GN_REMODELING_CART hit-rate bonus — 🚩
/// INFRA-DEFERRED: <c>PlayerEntity</c> does not yet carry a
/// <c>CartWeight</c> / <c>CartMaxWeight</c> pair nor a cart inventory.
/// For now we apply the non-player flat <c>+150</c> max bonus so the
/// damage stays consistent. Reroute once cart state lands on the PC.</para>
/// </summary>
public sealed class CartRevolution : RecursiveDamageSplashSkillImpl
{
    public CartRevolution() : base(SkillIds.MC_CARTREVOLUTION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 150;
}
