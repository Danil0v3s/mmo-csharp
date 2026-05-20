using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_GLORIA — Priest Gloria. Mirrors
/// <c>rathena-fork/src/map/skills/priest/gloria.cpp</c>.
///
/// Apply <see cref="StatusType.Gloria"/> on target (+30 Luk flat).
/// Duration <c>5 + 5*lv</c> seconds.
/// </summary>
public sealed class Gloria : SkillImpl
{
    public Gloria() : base(SkillIds.PR_GLORIA) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Sc?.Start(target, StatusType.Gloria, val1: 30, 0, 0, 0,
            durationMs: 5_000 + 5_000 * skillLevel, src);
    }
}
