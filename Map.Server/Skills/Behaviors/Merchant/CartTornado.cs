using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CART_TORNADO — Genetic Cart Tornado. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/carttornado.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>. Cart-weight + GN_REMODELING_CART
/// scale TODO.
/// </summary>
public sealed class CartTornado : RecursiveDamageSplashSkillImpl
{
    public CartTornado() : base(SkillIds.GN_CART_TORNADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + (-100 + 200 * skillLevel);
}
