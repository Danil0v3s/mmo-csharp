using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// GN_CART_TORNADO — Genetic Cart Tornado. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/carttornado.cpp</c>.
/// Ratio <c>+(-100 + 200*lv)</c>; when the caster carries
/// SC_BIONIC_WOODENWARRIOR the running ratio doubles.
///
/// <para>Cart-weight bonus + GN_REMODELING_CART hit-rate scale —
/// 🚩 INFRA-DEFERRED: cart inventory + skill-tree look-up for
/// <c>GN_REMODELING_CART</c> are not yet surfaced on
/// <see cref="PlayerEntity"/>. Reroute once cart state lands.</para>
/// </summary>
public sealed class CartTornado : RecursiveDamageSplashSkillImpl
{
    public CartTornado() : base(SkillIds.GN_CART_TORNADO) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var ratio = baseRatio + (-100 + 200 * skillLevel);
        if (ctx.Sc?.Get(src, StatusType.BionicWoodenwarrior) != null)
            ratio *= 2;
        return ratio;
    }
}
