using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Priest;

/// <summary>
/// PR_SUFFRAGIUM — Priest Suffragium. Mirrors
/// <c>rathena-fork/src/map/skills/priest/suffragium.cpp</c>.
///
/// Apply <see cref="StatusType.Suffragium"/> on target — next cast
/// time × (100 - 15*lv)%. Auto-consumed on cast (the consumption
/// hook lives in <see cref="Skills.SkillCastTimingService.CastFixSc"/>).
/// Duration <c>30 - 5*lv</c> seconds.
/// </summary>
public sealed class Suffragium : SkillImpl
{
    public Suffragium() : base(SkillIds.PR_SUFFRAGIUM) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var durationMs = 30_000 - 5_000 * skillLevel;
        ctx.Sc?.Start(target, StatusType.Suffragium, val1: skillLevel, 0, 0, 0, durationMs, src);
    }
}
