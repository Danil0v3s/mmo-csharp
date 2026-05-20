using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Whitesmith;

/// <summary>
/// WS_CARTBOOST — Whitesmith Cart Boost. Mirrors
/// <c>rathena-fork/src/map/skills/whitesmith/cartboost.cpp</c>.
///
/// Apply <see cref="StatusType.Cartboost"/> on caster — +MoveSpeed
/// while pushing cart. Requires cart equipped. Duration
/// <c>90 + 30*lv</c> seconds.
/// </summary>
public sealed class CartBoost : SkillImpl
{
    public CartBoost() : base(SkillIds.WS_CARTBOOST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Cartboost, val1: skillLevel, 0, 0, 0,
            durationMs: (90 + 30 * skillLevel) * 1000, src);
    }
}
