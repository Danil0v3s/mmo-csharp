using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// SM_ENDURE (id 8) — Swordsman Endure. rAthena
/// <c>skill.cpp:case SM_ENDURE</c> applies <see cref="StatusType.Endure"/>
/// on the caster with <c>Val1 = lv</c>, <c>Val2 = 7</c> (hit counter
/// before auto-expire). Duration <c>10000 + 10000 * lv</c> ms but rAthena
/// clamps the practical limit via the hit counter — Endure ends on the
/// 7th hit even if duration still has time, so the refresh-on-hit
/// infrastructure plugs in once it ports.
/// </summary>
public sealed class EndureBehavior : ISkillBehavior
{
    public ushort SkillId => SkillIds.SM_ENDURE;

    public bool Resolve(Entity source, Entity target, SkillDefinition def, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (ctx.Sc == null) return true;

        // Always self-target — Endure is a buff regardless of who the
        // client picked as target.
        var durationMs = 10_000 + 10_000 * skillLevel;
        ctx.Sc.Start(source, StatusType.Endure, val1: skillLevel, val2: 7, val3: 0, val4: 0,
            durationMs, source);
        return true;
    }
}
