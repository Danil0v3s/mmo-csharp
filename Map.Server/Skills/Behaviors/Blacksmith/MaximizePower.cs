using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Blacksmith;

/// <summary>
/// BS_MAXIMIZE — Blacksmith Maximize Power. Mirrors
/// <c>rathena-fork/src/map/skills/blacksmith/maximizepower.cpp</c>.
///
/// Apply <see cref="StatusType.Maximizepower"/> on the caster
/// (weapon rolls always max). Duration <c>60 * lv</c>s before
/// SP-drain consumes it.
/// </summary>
public sealed class MaximizePower : SkillImpl
{
    public MaximizePower() : base(SkillIds.BS_MAXIMIZE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(src, StatusType.Maximizepower, val1: skillLevel, 0, 0, 0,
            durationMs: 60_000 * skillLevel, src);
    }
}
