using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// BS_MAXIMIZE (id 114) — Blacksmith Maximize Power. rAthena
/// <c>skill.cpp:case BS_MAXIMIZE</c>: applies
/// <see cref="StatusType.Maximizepower"/> on the caster — all weapon
/// rolls go to MAX (always max-roll damage). Drains 1 SP per
/// <c>5 - lv*0.5</c> seconds (SP drain ports separately).
///
/// Duration <c>60_000 * lv</c> ms — capped by SP drain in practice.
/// </summary>
public sealed class MaximizePowerBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.BS_MAXIMIZE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 60_000 * skillLevel;
        ctx.Sc.Start(source, StatusType.Maximizepower, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
