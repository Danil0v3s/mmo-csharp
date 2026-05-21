using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MC_CARTREVOLUTION — Merchant Cart Revolution. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/cartrevolution.cpp</c>.
/// Ratio: <c>+50</c> base + cart-weight scale (1 % per 1 % loaded —
/// cart-weight read TODO; we use the non-player max of +150).
/// </summary>
public sealed class CartRevolution : RecursiveDamageSplashSkillImpl
{
    public CartRevolution() : base(SkillIds.MC_CARTREVOLUTION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 150;
}
