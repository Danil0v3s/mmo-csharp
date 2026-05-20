using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Sniper;

/// <summary>
/// SN_WINDWALK — Sniper Wind Walk. Mirrors
/// <c>rathena-fork/src/map/skills/sniper/windwalk.cpp</c>.
///
/// Apply <see cref="StatusType.Windwalk"/> on caster (party broadcast
/// pending). +Flee (2*lv) and +MoveSpeed. Duration <c>250 + 50*lv</c>s.
/// </summary>
public sealed class WindWalk : SkillImpl
{
    public WindWalk() : base(SkillIds.SN_WINDWALK) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Windwalk, val1: skillLevel, 0, 0, 0,
            durationMs: (250 + 50 * skillLevel) * 1000, src);
    }
}
