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
        // rAthena: skill_check_unit_movepos teleports the caster near (x,y).
        // Deferred: explicit GvG/BG gating — the C# IUnitOpsService.CheckUnitMovePos
        // contract does not yet document the mapflag refusal that rAthena
        // performs ("You don't move on GVG grounds."); when that gate is added
        // server-side, this call honors it automatically.
        ctx.UnitOps?.CheckUnitMovePos(src, x, y, 1);
        ctx.Sc?.End(src, StatusType.Hiding);
    }
}
