using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// PR_MAGNIFICAT (id 74) — Priest Magnificat. rAthena
/// <c>skill.cpp:case PR_MAGNIFICAT</c>: applies
/// <see cref="StatusType.Magnificat"/> on the caster (party-wide in
/// rAthena's actual implementation, but party broadcast lands when
/// the party-target hook ports — self-only for first wave).
///
/// Effect: +100 % SP regen (T2.4b+ reads the SC in NaturalHealService
/// for the SP regen multiplier). Duration <c>30 * lv</c> seconds.
/// </summary>
public sealed class MagnificatBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.PR_MAGNIFICAT;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;
        var durationMs = 30_000 * skillLevel;
        // Always self-target (rAthena: skill_target == self for Magnificat).
        ctx.Sc.Start(source, StatusType.Magnificat, val1: skillLevel, 0, 0, 0,
            durationMs, source);
        return true;
    }
}
