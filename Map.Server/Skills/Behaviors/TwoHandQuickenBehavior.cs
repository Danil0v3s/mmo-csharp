using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// KN_TWOHANDQUICKEN (id 60) — Knight Two-Hand Quicken. rAthena
/// <c>skill.cpp:case KN_TWOHANDQUICKEN</c> applies
/// <see cref="StatusType.Twohandquicken"/> on the caster with
/// <c>Val1 = lv</c>. AspdRate boost flows via the SC handler
/// (lv * 7 ASPD-rate per level in renewal). Duration 30 * lv seconds.
/// </summary>
public sealed class TwoHandQuickenBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.KN_TWOHANDQUICKEN;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Val1 = level for ASPD math; the SC handler reads it.
        var aspdBoost = 7 * skillLevel;
        var durationMs = 30_000 * skillLevel;
        ctx.Sc.Start(source, StatusType.Twohandquicken, val1: aspdBoost, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
