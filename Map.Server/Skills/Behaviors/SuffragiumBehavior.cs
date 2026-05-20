using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_SUFFRAGIUM (id 67) — Priest Suffragium. rAthena
/// <c>skill.cpp:case PR_SUFFRAGIUM</c>: applies
/// <see cref="StatusType.Suffragium"/> on the target with
/// <c>Val1 = lv</c>. Next single cast's variable time × (100 - 15*lv)%
/// then SC auto-consumes (cast-side consumption is wired in
/// SkillCastTimingService.CastFixSc).
///
/// Duration <c>30_000 - 5000*lv</c> ms (30 s at lv1, 15 s at lv3).
/// </summary>
public sealed class SuffragiumBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_SUFFRAGIUM;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 30_000 - 5_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Suffragium, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
