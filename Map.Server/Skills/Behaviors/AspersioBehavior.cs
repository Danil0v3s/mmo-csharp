using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_ASPERSIO (id 68) — Priest Aspersio. rAthena
/// <c>skill.cpp:case PR_ASPERSIO</c>: applies
/// <see cref="StatusType.Aspersio"/> on the target — the target's
/// weapon attacks become Holy element for the SC duration. Requires
/// the caster to carry a Holy Water (rAthena consumes one per cast;
/// requirement check ports separately).
///
/// Duration <c>180_000 + 60_000 * lv</c> ms in renewal.
/// </summary>
public sealed class AspersioBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_ASPERSIO;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 180_000 + 60_000 * skillLevel;
        ctx.Sc.Start(target, StatusType.Aspersio, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
