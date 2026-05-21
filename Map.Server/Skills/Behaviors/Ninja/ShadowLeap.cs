using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// NJ_SHADOWJUMP — Shadow Leap. Manual port of
/// <c>rathena-fork/src/map/skills/ninja/shadowleap.cpp</c>.
/// Teleport to (x, y) and end SC_HIDING.
/// </summary>
public sealed class ShadowLeap : SkillImpl
{
    public ShadowLeap() : base(SkillIds.NJ_SHADOWJUMP) { }

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // TODO: skill_check_unit_movepos teleport (gated outside GvG/BG).
        ctx.Sc?.End(src, StatusType.Hiding);
    }
}
