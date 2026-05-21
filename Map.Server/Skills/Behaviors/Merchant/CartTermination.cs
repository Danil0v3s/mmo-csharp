using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// WS_CARTTERMINATION — Whitesmith Cart Termination. Manual port of
/// <c>rathena-fork/src/map/skills/merchant/carttermination.cpp</c>.
/// Ratio scales by cart-weight; we use the non-player max
/// (<c>80000 / (10*(16-lv)) - 100</c>). On-hit SC_STUN at <c>5*lv %</c>.
/// </summary>
public sealed class CartTermination : WeaponSkillImpl
{
    private readonly Random _rng;

    public CartTermination() : base(SkillIds.WS_CARTTERMINATION) => _rng = Random.Shared;

    public CartTermination(Random? rng = null) : base(SkillIds.WS_CARTTERMINATION) => _rng = rng ?? Random.Shared;

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var i = Math.Max(1, 10 * (16 - skillLevel));
        return baseRatio + (80000 / i - 100);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (_rng.Next(100) < 5 * skillLevel)
            ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 3000, src);
    }
}
