using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_GLORIA (id 75) — Priest Gloria. rAthena
/// <c>skill.cpp:case PR_GLORIA</c>: applies <see cref="StatusType.Gloria"/>
/// on the target with <c>Val1 = 30</c> (+30 Luk flat). Duration
/// <c>5_000 + 5_000 * lv</c> ms in renewal (short — ~5-30s).
/// </summary>
public sealed class GloriaBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_GLORIA;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 5_000 + 5_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Gloria, val1: 30, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
